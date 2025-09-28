using System.Diagnostics.CodeAnalysis;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Enum;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;
using Engine.Module.Rendering.Computers;
using Engine.Module.Rendering.Renderers;
using Engine.Module.Rendering.Renderers.Debug;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Renderers.Lamina;
using Engine.Module.Rendering.Utilities;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using MapFlags = Diligent.MapFlags;
using ResourceDimension = Diligent.ResourceDimension;
using Usage = Diligent.Usage;

namespace Engine.Module.Rendering;

[UsedImplicitly]
public class RenderingHost : IRenderingHost
{
    private List<Backstage> _backstages = [];
    
    // Computers
    private CullingComputer _cullingComputer = null!;
    
    // Renderers
    private DebugFramerateFrameRenderer _debugFramerateFrameRenderer = null!;
    private DebugLogFrameRenderer _debugLogFrameRenderer = null!;
    private DebugProfilerFrameRenderer _debugProfilerFrameRenderer = null!;
    private LaminaRenderer _laminaRenderer = null!;
    private SceneRenderer _sceneRenderer = null!;
    internal TextRenderer ImmediateTextRenderer = null!;
    
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;

    private ITexture _renderTarget = null!;
    private ITexture _renderDepth = null!;
    private ITextureView _renderTargetView = null!;
    private ITextureView _renderDepthView = null!;
    
    public IRootHypervisor Hypervisor { get; set; } = null!;
    internal IWindow RootWindow => Hypervisor.Window;

    private Vector2D<int> FramebufferSize => RootWindow.GetScaledFramebufferSize();

    // Constant camera matrix buffer
    private IBuffer _viewMatrixBuffer = null!;
    
    // Contains the index of the current instance
    private IBuffer _instanceIndexBuffer = null!;
    // Holds the per-instance data for all instances
    private InfiniteInstanceWriteOnlyBuffer<InstanceData> _infiniteInstanceBuffer = null!;

    public void InitializeResources(RenderingResources resources)
    {
        _renderDevice = resources.RenderDevice;
        _immediateContext = resources.ImmediateContext;
        _deferredContexts = resources.DeferredContexts;
        _swapChain = resources.SwapChain;
        
        _viewMatrixBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "CameraViewMatrixBuffer",
            Size = MatrixFloat.SizeInBytes,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        });
        
        _instanceIndexBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "ObjectIndexBuffer",
            Size = 16u,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
        });
        
        using var engineFactory = Native.GetEngineFactoryD3D12();
        _infiniteInstanceBuffer = new InfiniteInstanceWriteOnlyBuffer<InstanceData>();
        var renderContext = new RenderContext
        {
            ImmediateContext = _immediateContext,
            DeferredContexts = _deferredContexts,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
            ViewMatrixBuffer = _viewMatrixBuffer,
            ObjectIndexBuffer = _instanceIndexBuffer,
            InstanceBuffer = _infiniteInstanceBuffer,
            RenderTargetSize = new Vector2(_swapChain.GetDesc().Width, _swapChain.GetDesc().Height),
            ShaderFactory = engineFactory.CreateDefaultShaderSourceStreamFactory("Assets/Shaders")
        };
        
        RenderContext.Current = renderContext;
    }
    
    public void InitializeRenderers()
    {
        // Computers
        _cullingComputer = new CullingComputer(this);
        
        // Renderers
        _debugFramerateFrameRenderer = new DebugFramerateFrameRenderer(this);
        _debugLogFrameRenderer = new DebugLogFrameRenderer(this);
        _debugProfilerFrameRenderer = new DebugProfilerFrameRenderer(this);
        _laminaRenderer = new LaminaRenderer(this, _deferredContexts[0]);
        _sceneRenderer = new SceneRenderer(this);
        ImmediateTextRenderer = new TextRenderer(_immediateContext);
        
        CreateRenderTargets();
        
        RootWindow.Render += RenderSingleFrameSync;
        RootWindow.FramebufferResize += OnFramebufferResize;
    }

    private void CreateRenderTargets()
    {
        var swapChain = _swapChain.GetDesc();
        _renderTarget = _renderDevice.CreateTexture(new TextureDesc
        {
            Name = "MSAA Color",
            Type = ResourceDimension.Tex2d,
            Width = (uint)FramebufferSize.X,
            Height = (uint)FramebufferSize.Y,
            MipLevels = 1,
            Format = swapChain.ColorBufferFormat,
            SampleCount = (uint)PipelineBuilder.MsaaSamples,
            BindFlags = BindFlags.RenderTarget
        });
        _renderTargetView = _renderTarget.GetDefaultView(TextureViewType.RenderTarget);
        
        _renderDepth = _renderDevice.CreateTexture(new TextureDesc
        {
            Name        = "MSAA Depth",
            Type        = ResourceDimension.Tex2d,
            Width       = (uint)FramebufferSize.X,
            Height      = (uint)FramebufferSize.Y,
            MipLevels   = 1,
            Format      = swapChain.DepthBufferFormat,
            SampleCount = (uint)PipelineBuilder.MsaaSamples,
            BindFlags   = BindFlags.DepthStencil
        });
        _renderDepthView = _renderDepth.GetDefaultView(TextureViewType.DepthStencil);
    }
    
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    private void DisposeRenderTargets()
    {
        _renderTarget?.Dispose();
        _renderDepth?.Dispose();
    }

    private int _atomsToRenderCount;
    private IRenderable[] _atomsToRender = [];
    private int _widgetsToRenderCount;
    private ILaminaRenderable[] _widgetsToRender = [];

    private void RenderSingleFrameSync(double deltaTime)
    {
        RenderSingleFrame(deltaTime).GetAwaiter().GetResult();
    }

    public void RenderEngineLoadingScreen()
    {
        CreateRenderTargets();
        
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.0, 0.0, 0.0, 1.0), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        
        SplashRenderer.RenderOnce();

        var rtv = _swapChain.GetCurrentBackBufferRTV();
        var rtvTexture = rtv.GetTexture();
        
        _immediateContext.ResolveTextureSubresource(
            _renderTarget,
            rtvTexture,
            new ResolveTextureSubresourceAttribs
            {
                Format = _swapChain.GetDesc().ColorBufferFormat,
                SrcTextureTransitionMode = ResourceStateTransitionMode.Transition,
                DstTextureTransitionMode = ResourceStateTransitionMode.Transition,
            }
        );
        
        _swapChain.Present(0);
    }
    
    public async Task RenderSingleFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingGpuWait);
        
        stopwatch = Profiler.Start();
        RenderStats.Reset();
        FrameCounter.Increment();
        _immediateContext.ClearStats();
        
        await PrepareRenderers();
        
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        
        await InvokeSceneRenderers(deltaTime);
        
        var commandList = await InvokeLaminaRenderers(deltaTime);
        if (commandList is not null)
            _immediateContext.ExecuteCommandLists([commandList]);
        
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        var ctx = RenderContext.Current;
        ctx.RenderTargetSize = new Vector2(_swapChain.GetDesc().Width, _swapChain.GetDesc().Height);
        RenderContext.Current = ctx;
        
        var rtv = _swapChain.GetCurrentBackBufferRTV();
        var rtvTexture = rtv.GetTexture();
        
        _immediateContext.ResolveTextureSubresource(
            _renderTarget,
            rtvTexture,
            new ResolveTextureSubresourceAttribs
            {
                Format = _swapChain.GetDesc().ColorBufferFormat,
                SrcTextureTransitionMode = ResourceStateTransitionMode.Transition,
                DstTextureTransitionMode = ResourceStateTransitionMode.Transition,
            }
        );
        
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingFullFrame);
        _swapChain.Present(0);
    }

    private Task PrepareRenderers()
    {
        Camera? activeCamera = null;
        foreach (var backstage in _backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
        }
        
        Camera.Plane[] frustumPlanes = [];
        var wvpMatrix = Matrix.Identity;
        if (activeCamera != null)
        {
            frustumPlanes = activeCamera.UpdateFrustumPlanes();
            wvpMatrix = activeCamera.AsCameraView().ToMatrix();
        }
        
        var mapUniformBuffer = _immediateContext.MapBuffer<Vector4Float>(_viewMatrixBuffer, MapType.Write, MapFlags.Discard);
        mapUniformBuffer[0] = wvpMatrix[0].Downgrade();
        mapUniformBuffer[1] = wvpMatrix[1].Downgrade();
        mapUniformBuffer[2] = wvpMatrix[2].Downgrade();
        mapUniformBuffer[3] = wvpMatrix[3].Downgrade();
        mapUniformBuffer[4] = new Vector4Float(FramebufferSize.X, FramebufferSize.Y, 1.0f / FramebufferSize.X, 1.0f / FramebufferSize.Y);
        _immediateContext.UnmapBuffer(_viewMatrixBuffer, MapType.Write);
        
        _infiniteInstanceBuffer.FrameStart();
        _cullingComputer.ReadResultsAndPrepare();
        if (activeCamera == null)
            return Task.CompletedTask;
        
        foreach (var backstage in _backstages)
        {
            // Collect all IRenderable entities reachable from the atom tree
            var stopwatch = Profiler.Start();
            CollectAtomsToRender(backstage);
            stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCollectAtoms);
        }
        _cullingComputer.SubmitCurrentQueue(frustumPlanes);
        return Task.CompletedTask;
    }
    
    private Task<ICommandList?> InvokeLaminaRenderers(double deltaTime)
    {
        var commandList = _laminaRenderer.RenderRetainedTexturesWithTiming(_widgetsToRender, _widgetsToRenderCount);
        _widgetsToRenderCount = 0;
        Array.Clear(_widgetsToRender);
        return Task.FromResult(commandList);
    }
    
    private Task InvokeSceneRenderers(double deltaTime)
    {
        _sceneRenderer.RenderAtomTree(_atomsToRender, _atomsToRenderCount);
        _atomsToRenderCount = 0;
        Array.Clear(_atomsToRender);
        
        _debugFramerateFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugLogFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugProfilerFrameRenderer.RenderFrameWithTiming(deltaTime);

        ImmediateTextRenderer.Flush();
        return Task.CompletedTask;
    }

    private Camera? FindActiveCamera(Atom target)
    {
        if (target is Camera camera
            && ((camera.IsEditorCamera && GameplayContext == GameplayContext.Editor) || (!camera.IsEditorCamera && GameplayContext != GameplayContext.Editor)))
        {
            return camera;
        }

        foreach (var child in target.Children)
        {
            var foundCamera = FindActiveCamera(child); 
            if (foundCamera != null)
                return foundCamera;
        }

        return null;
    }

    private void CollectAtomsToRender(Atom target)
    {
        if (!Atom.IsValid(target))
            return;

        var instanceCount = GetInstanceCount(target);
        
        if (target is ICullable cullable)
        {
            _cullingComputer.QueueForCulling(cullable);
            if (!_cullingComputer.IsVisible(cullable))
            {
                RenderStats.InstancesCulled += instanceCount;
                return;
            }
        }

        if (target is IRenderable renderable)
        {
            // Resize the array if necessary
            if (_atomsToRenderCount >= _atomsToRender.Length)
                Array.Resize(ref _atomsToRender, Math.Max(_atomsToRenderCount + 1, _atomsToRender.Length * 2));
            
            RenderStats.InstancesDrawn += instanceCount;
            _atomsToRender[_atomsToRenderCount++] = renderable;
        }
        if (target is ILaminaRenderable { Dirty: true } laminaRenderable)
        {
            if (_widgetsToRenderCount >= _widgetsToRender.Length)
                Array.Resize(ref _widgetsToRender, Math.Max(_widgetsToRenderCount + 1, _widgetsToRender.Length * 2));
            
            _widgetsToRender[_widgetsToRenderCount++] = laminaRenderable;
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(child);
        }
    }

    private int GetInstanceCount(Atom target)
    {
        return target is IRenderable renderable ? renderable.ProduceRenderRequest().InstanceCount : 1;
    }
    
    private void OnFramebufferResize(Vector2D<int> size)
    {
        var width = size.X;
        var height = size.Y;

        if (width == 0 || height == 0)
            return;
        
        DisposeRenderTargets();
        _swapChain.Resize((uint)size.X, (uint)size.Y, SurfaceTransform.Identity);
        CreateRenderTargets();
        AssetManager.Shared.Pipelines.InvalidateAll();
    }

    public void ToggleLogRendering() => _debugLogFrameRenderer.OnToggleMode();

    private GameplayContext GameplayContext => Hypervisor.GameplayContext;

    public void HotShutdown()
    { 
        RootWindow.Render -= RenderSingleFrameSync;
        RootWindow.FramebufferResize -= OnFramebufferResize;
        try
        {
            _immediateContext.SetPipelineState(null);
            _immediateContext.Flush();
            _renderDevice.IdleGPU();
            AssetManager.Shared.Pipelines.InvalidateAll();
        }
        catch { /* ignored */ }

        ImmediateTextRenderer.Dispose();
        _backstages = [];
        _atomsToRender = [];
        _viewMatrixBuffer.Dispose();
        _instanceIndexBuffer.Dispose();
        _infiniteInstanceBuffer.Dispose();
        _laminaRenderer.Dispose();
        DisposeRenderTargets();
    }

    public void NotifyModuleReloaded(EngineModule module)
    {
        _backstages.Clear();
        if (Hypervisor.GameplayModule is Backstage gameplayHost)             
            _backstages.Add(gameplayHost);
        if (Hypervisor.WorkspaceModule is Backstage workspaceHost)
            _backstages.Add(workspaceHost);
    }
    public void NotifyGameplayContextChanged(GameplayContext context) {}
    
    // public long Register(LaminaRenderer renderer)
    // {
    //     // if (maskedParent is not Spatial parent)
    //     //     throw new ArgumentException("Unable to unmask a Spatial"); 
    //     // if (maskedComponent is not PhysicsComponent component)
    //     //     throw new ArgumentException("Unable to unmask a PhysicsComponent");
    //     return _registeredAtoms.Add(parent, component);
    // }
    // public void Unregister(long rid) => _registeredAtoms.Remove(rid);
}
