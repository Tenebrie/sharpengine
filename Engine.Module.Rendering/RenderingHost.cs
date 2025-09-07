using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.Enum;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using Engine.Core.Memory;
using Engine.Core.Modules;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;
using Engine.Module.Rendering.Computers;
using Engine.Module.Rendering.Renderers;
using Engine.Module.Rendering.Renderers.Debug;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Utilities;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Vortice.Direct3D12;
using Vortice.DXGI;
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
    private DebugFramerateRenderer _debugFramerateRenderer = null!;
    private DebugLogRenderer _debugLogRenderer = null!;
    private DebugProfilerRenderer _debugProfilerRenderer = null!;
    internal TextRenderer TextRenderer = null!;
    
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

    private float BaseResolutionScale => (float)RootWindow.Size.X / RootWindow.FramebufferSize.X;
    private float ResolutionScale => BaseResolutionScale * 1.0f;

    private Vector2D<int> FramebufferSize => new(
        (int)Math.Round(RootWindow.FramebufferSize.X * ResolutionScale),
        (int)Math.Round(RootWindow.FramebufferSize.Y * ResolutionScale)
    );

    // Constant camera matrix buffer
    private IBuffer _viewMatrixBuffer = null!;
    
    // Contains the index of the current instance
    private IBuffer _instanceIndexBuffer = null!;
    // Holds the per-instance data for all instances
    private InfiniteInstanceWriteOnlyBuffer<InstanceData> _infiniteInstanceBuffer = null!;
    
    public void HotInitialize(RenderingResources resources)
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
            DeviceContext = _immediateContext,
            DeferredContexts = _deferredContexts,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
            ViewMatrixBuffer = _viewMatrixBuffer,
            ObjectIndexBuffer = _instanceIndexBuffer,
            InstanceBuffer = _infiniteInstanceBuffer,
            ShaderFactory = engineFactory.CreateDefaultShaderSourceStreamFactory("Assets/Shaders")
        };
        
        RenderContext.Current = renderContext;
        
        // Computers
        _cullingComputer = new CullingComputer(this);
        
        // Renderers
        _debugFramerateRenderer = new DebugFramerateRenderer(this);
        _debugLogRenderer = new DebugLogRenderer(this);
        _debugProfilerRenderer = new DebugProfilerRenderer(this);
        TextRenderer = new TextRenderer();

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

    private void RenderSingleFrameSync(double deltaTime)
    {
        RenderSingleFrame(deltaTime).GetAwaiter().GetResult();
    }

    public void RenderEngineLoadingScreen()
    {
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.0, 0.0, 0.0, 1.0), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        
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
        
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);

        await PrepareRenderers(deltaTime);
        await InvokeRenderers(deltaTime);
        
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

    private Task PrepareRenderers(double deltaTime)
    {
        Camera? activeCamera = null;
        foreach (var backstage in _backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
        }
        
        Camera.Plane[] frustumPlanes = [];
        if (activeCamera == null)
        {
            // Logger.Error("No active camera found for rendering.");
        }
        else
        {
            frustumPlanes = activeCamera.UpdateFrustumPlanes();
        
            var wvpMatrix = activeCamera.AsCameraView().ToMatrix();
            var mapUniformBuffer = _immediateContext.MapBuffer<MatrixFloat>(_viewMatrixBuffer, MapType.Write, MapFlags.Discard);
            mapUniformBuffer[0] = wvpMatrix.Downgrade();
            _immediateContext.UnmapBuffer(_viewMatrixBuffer, MapType.Write);
        }
        
        _infiniteInstanceBuffer.FrameStart();
        _cullingComputer.ReadResultsAndPrepare();
        if (activeCamera == null)
            return Task.CompletedTask;
        
        foreach (var backstage in _backstages)
        {
            // Collect all IRenderable entities reachable from the atom tree
            var stopwatch = Profiler.Start();
            CollectAtomsToRender(ref _atomsToRender, ref _atomsToRenderCount, backstage);
            stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCollectAtoms);
        }
        _cullingComputer.SubmitCurrentQueue(frustumPlanes);
        return Task.CompletedTask;
    }
    
    private Task InvokeRenderers(double deltaTime)
    {
        RenderAtomTree(_atomsToRender, _atomsToRenderCount);
        _atomsToRenderCount = 0;
        Array.Clear(_atomsToRender);
        
        _debugFramerateRenderer.RenderFrameWithTiming(deltaTime);
        _debugLogRenderer.RenderFrameWithTiming(deltaTime);
        _debugProfilerRenderer.RenderFrameWithTiming(deltaTime);

        TextRenderer.Flush();
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

    private void CollectAtomsToRender(
        ref IRenderable[] entitiesToRender,
        ref int entitiesToRenderCount,
        Atom target)
    {
        if (!Atom.IsValid(target))
            return;

        if (target is IRenderable renderable)
        {
            // Resize the array if necessary
            if (entitiesToRenderCount >= entitiesToRender.Length)
                Array.Resize(ref entitiesToRender, Math.Max(entitiesToRenderCount + 1, entitiesToRender.Length * 2));

            _cullingComputer.QueueForCulling(renderable);
            var instanceCount = renderable.ProduceRenderRequest().InstanceCount;
            if (_cullingComputer.IsVisible(renderable))
            {
                RenderStats.InstancesDrawn += instanceCount;
                entitiesToRender[entitiesToRenderCount++] = renderable;
            }
            else
            {
                RenderStats.InstancesCulled += instanceCount;
                return;
            }
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(ref entitiesToRender, ref entitiesToRenderCount, child);
        }
    }

    private readonly Dictionary<int, MergedRenderRequest> _renderRequestPool = new();
    
    /**
     * TODO: Instead of dynamically collecting the requests every frame, consider keeping them around, and rely on registration/deregistration
     * when entities are added/removed or their materials/meshes change.
     */
    private struct MergedRenderRequest
    {
        public required StaticMesh Mesh;
        public required Material Material;
        public required IRenderScript RenderScript;
    
        public required int InstanceCount;
        public required MemoryManager.ArrayHandle InstanceTransforms;
        public required MemoryManager.ArrayHandle MaterialInstances;
    
        public static MergedRenderRequest Create(RenderRequest request)
        { 
            var req = new MergedRenderRequest  
            {
                Mesh = request.Mesh, 
                Material = request.Material, 
                RenderScript = request.RenderScript,
                InstanceCount = request.InstanceCount,
                InstanceTransforms = 
                    MemoryManager.ProduceArray<Transform>(MemoryDomain.Rendering, request.InstanceCount),
                MaterialInstances =
                    MemoryManager.ProduceArray<MaterialInstance>(MemoryDomain.Rendering, request.InstanceCount),
            };

            req.MaterialInstances = MemoryManager.MergeArrays(MemoryDomain.Rendering, req.MaterialInstances,
                request.InstanceCount, request.MaterialInstances);
            req.InstanceTransforms = MemoryManager.MergeArrays(MemoryDomain.Rendering, req.InstanceTransforms,
                request.InstanceCount, request.InstanceTransforms);
            return req;
        }
    }

    private void RenderAtomTree(IRenderable[] atomsToRender, int atomsToRenderCount)
    {
        _renderRequestPool.Clear();

        var stopwatch = Profiler.Start();
        for (var i = 0; i < atomsToRenderCount; i++)
        {
            var renderable = atomsToRender[i];
            var request = renderable.ProduceRenderRequest();
            if (!_renderRequestPool.TryGetValue(request.HashCode, out var mergedRequest))
            {
                _renderRequestPool[request.HashCode] = MergedRenderRequest.Create(request);
                continue;
            }
            
            mergedRequest.MaterialInstances = MemoryManager.MergeArrays(MemoryDomain.Rendering,
                mergedRequest.MaterialInstances,
                request.InstanceCount, request.MaterialInstances
            );
            mergedRequest.InstanceTransforms = MemoryManager.MergeArrays(MemoryDomain.Rendering,
                mergedRequest.InstanceTransforms,
                request.InstanceCount, request.InstanceTransforms
            );
            mergedRequest.InstanceCount += request.InstanceCount;
            _renderRequestPool[request.HashCode] = mergedRequest;
        }
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCombineRequests);
        
        stopwatch = Profiler.Start();
        RenderStats.DrawCalls += _renderRequestPool.Values.Count;
        foreach (var req in _renderRequestPool.Values)
        {
            req.RenderScript.Render(_immediateContext, req.InstanceCount, req.Mesh, (Transform[])req.InstanceTransforms.Array, req.Material,
                (MaterialInstance[])req.MaterialInstances.Array);
        }
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingSubmitAtoms);
        MemoryManager.FreeDomain(MemoryDomain.Rendering);
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

    public void ToggleLogRendering() => _debugLogRenderer.OnToggleMode();

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

        TextRenderer.Dispose();
        _backstages = [];
        _atomsToRender = [];
        _viewMatrixBuffer.Dispose();
        _instanceIndexBuffer.Dispose();
        _infiniteInstanceBuffer.Dispose();
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
}
