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
using Engine.Core.Enum;
using Engine.Module.Rendering.Fonts;
using Engine.Module.Rendering.Renderers;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using Engine.Core.Memory;
using Engine.Core.Modules;
using Engine.Core.Profiling;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Module.Rendering;

/**
 * Excluded from hot reload - restart the application to apply changes in this class.
 */
[UsedImplicitly]
public class RenderingModuleBootstrap : IRenderingModuleBootstrap
{
    public required IRootHypervisor Hypervisor { get; set; }
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;
    private IEngineFactoryD3D12 _engineFactory = null!;
    private static IEngineFactory.MessageCallbackDelegate _messageCallback = null!;
    private static GCHandle _sCallbackRoot;
    
    public RenderingResources Initialize()
    {
        _engineFactory = Native.GetEngineFactoryD3D12();
        SetMessageCallback(_engineFactory);
        CreateRenderDeviceAndSwapChain(
            _engineFactory,
            out var renderDeviceOut,
            out _immediateContext,
            out _deferredContexts,
            out var swapChainOut,
            Hypervisor.Window
        );
        _renderDevice = renderDeviceOut;
        _swapChain = swapChainOut;

        return new RenderingResources
        {
            EngineFactory = _engineFactory,
            ImmediateContext = _immediateContext,
            DeferredContexts = _deferredContexts,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
        };
    }
    
    private static void SetMessageCallback(IEngineFactory engineFactory)
    {
        _messageCallback = (severity, message, function, file, line) =>
        {
            switch (severity)
            {
                case DebugMessageSeverity.Warning:
                case DebugMessageSeverity.Error:
                case DebugMessageSeverity.FatalError:
                    Console.WriteLine($"Diligent Engine: {severity} in {function}() ({file}, {line}): {message}");
                    break;
                case DebugMessageSeverity.Info:
                    Console.WriteLine($"Diligent Engine: {severity} {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        };
        _sCallbackRoot = GCHandle.Alloc(_messageCallback, GCHandleType.Normal);
        engineFactory.SetMessageCallback(_messageCallback);
    }

    private static void CreateRenderDeviceAndSwapChain(
        IEngineFactoryD3D12 engineFactory,
        out IRenderDevice renderDevice,
        out IDeviceContext immediateContext,
        out IDeviceContext[] deferredContexts,
        out ISwapChain swapChain,
        IWindow window)
    {
        engineFactory.CreateDeviceAndContextsD3D12(new EngineD3D12CreateInfo
        {
            EnableValidation = true,
            NumDeferredContexts = 8
        }, out renderDevice, out IDeviceContext[] contextsOut);
        
        immediateContext = contextsOut[0];
        deferredContexts = contextsOut.Skip(1).ToArray();
        
        swapChain = engineFactory.CreateSwapChainD3D12(
            renderDevice,
            immediateContext,
            new SwapChainDesc(),
            new FullScreenModeDesc(),
            new Win32NativeWindow
            {
                Wnd = window.Native!.Win32!.Value.Hwnd
            });
    }

    public void Shutdown()
    {
        if (_swapChain == null)
        {
            Console.Error.WriteLine("SwapChain is already null. Something is wrong :(");
            return;
        }
        _swapChain.Present(0);
        
        AssemblyAssetManager.DisposeAll();
        _swapChain.Dispose();
        _immediateContext.Dispose();
        _renderDevice.Dispose();
    }
}

[UsedImplicitly]
public class RenderingHost : IRenderingHost
{
    private List<Backstage> _backstages = [];
    private LogRenderer _logRenderer = null!;
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

    private static int MsaaSamples => 8;

    private IBuffer _viewMatrixBuffer = null!;
    private IBuffer _objectIndexBuffer = null!;
    private InfiniteInstanceBuffer _infiniteInstanceBuffer = null!;
    
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
        
        _objectIndexBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "ObjectIndexBuffer",
            Size = 16u,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
        });
                
        using var engineFactory = Native.GetEngineFactoryD3D12();
        _infiniteInstanceBuffer = new InfiniteInstanceBuffer();
        var renderContext = new RenderContext
        {
            DeviceContext = _immediateContext,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
            ViewMatrixBuffer = _viewMatrixBuffer,
            ObjectIndexBuffer = _objectIndexBuffer,
            InstanceBuffer = _infiniteInstanceBuffer,
            ShaderFactory = engineFactory.CreateDefaultShaderSourceStreamFactory("Assets/Shaders")
        };
        
        RenderContext.Current = renderContext;
        
        _logRenderer = new LogRenderer(this);
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
            Width = swapChain.Width,
            Height = swapChain.Height,
            MipLevels = 1,
            Format = swapChain.ColorBufferFormat,
            SampleCount = (uint)MsaaSamples,
            BindFlags = BindFlags.RenderTarget
        });
        _renderTargetView = _renderTarget.GetDefaultView(TextureViewType.RenderTarget);
        
        _renderDepth = _renderDevice.CreateTexture(new TextureDesc
        {
            Name        = "MSAA Depth",
            Type        = ResourceDimension.Tex2d,
            Width       = swapChain.Width,
            Height      = swapChain.Height,
            MipLevels   = 1,
            Format      = swapChain.DepthBufferFormat,
            SampleCount = (uint)MsaaSamples,
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
        FrameCounter.Increment(); 
        
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);

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
        
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingPrepare);
        _swapChain.Present(1);
    }

    private Task InvokeRenderers(double deltaTime)
    {
        Camera? activeCamera = null;
        foreach (var backstage in _backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
        }
        
        if (activeCamera == null)
        {
            // Logger.Error("No active camera found for rendering.");
        }
        else
        {
            activeCamera.UpdateFrustumPlanes();
        
            var wvpMatrix = activeCamera.AsCameraView().ToMatrix();
            var mapUniformBuffer = _immediateContext.MapBuffer<MatrixFloat>(_viewMatrixBuffer, MapType.Write, MapFlags.Discard);
            mapUniformBuffer[0] = wvpMatrix.Downgrade();
            _immediateContext.UnmapBuffer(_viewMatrixBuffer, MapType.Write);
        }
        
        _infiniteInstanceBuffer.FrameStart();
        
        _logRenderer.RenderFrame(deltaTime);
        if (activeCamera != null)
        {
            foreach (var backstage in _backstages)
            {
                // Collect all IRenderable entities reachable from the atom tree
                CollectAtomsToRender(activeCamera, ref _atomsToRender, ref _atomsToRenderCount, backstage);
                // Render surviving atoms
                RenderAtomTree(_atomsToRender, _atomsToRenderCount);

                _atomsToRenderCount = 0;
                Array.Clear(_atomsToRender);
            }
        }

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

    private static void CollectAtomsToRender(
        Camera activeCamera,
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

            renderable.PerformCulling(activeCamera);
            if (renderable.IsOnScreen)
                entitiesToRender[entitiesToRenderCount++] = renderable;
            else
                return;
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(activeCamera, ref entitiesToRender, ref entitiesToRenderCount, child);
        }
    }

    private readonly Dictionary<int, MergedRenderRequest> _renderRequestPool = new();
    
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

        for (var i = 0; i < atomsToRenderCount; i++)
        {
            var renderable = atomsToRender[i];
            var request = renderable.Render();
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
        
        foreach (var req in _renderRequestPool.Values)
        {
            req.RenderScript.Render(_immediateContext, req.InstanceCount, req.Mesh, (Transform[])req.InstanceTransforms.Array, req.Material,
                (MaterialInstance[])req.MaterialInstances.Array);
        }
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

    public void ToggleLogRendering() => _logRenderer.OnToggleMode();

    private GameplayContext GameplayContext => Hypervisor.GameplayContext;

    public void HotShutdown()
    { 
        RootWindow.Render -= RenderSingleFrameSync;
        RootWindow.FramebufferResize -= OnFramebufferResize;
        _immediateContext.SetPipelineState(null);
        _immediateContext.Flush();
        _renderDevice.IdleGPU();
        AssetManager.Shared.Pipelines.InvalidateAll();
        TextRenderer.Dispose();
        _backstages = [];
        _atomsToRender = [];
        _viewMatrixBuffer.Dispose();
        _objectIndexBuffer.Dispose();
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
