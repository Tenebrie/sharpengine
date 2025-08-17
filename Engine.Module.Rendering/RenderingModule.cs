using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Enum;
using Engine.Module.Rendering.Fonts;
using Engine.Module.Rendering.Renderers;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;
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
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;
    private IEngineFactoryD3D12 _engineFactory = null!;
    private static IEngineFactory.MessageCallbackDelegate _messageCallback = null!;
    private static GCHandle _sCallbackRoot;
    
    public RenderingResources Initialize(IWindow window)
    {
        _engineFactory = Native.GetEngineFactoryD3D12();
        SetMessageCallback(_engineFactory);
        CreateRenderDeviceAndSwapChain(
            _engineFactory,
            out var renderDeviceOut,
            out _immediateContext,
            out _deferredContexts,
            out var swapChainOut,
            window
        );
        _renderDevice = renderDeviceOut;
        _swapChain = swapChainOut;

        return new RenderingResources
        {
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
        _swapChain.Dispose();
        _immediateContext.Dispose();
        _renderDevice.Dispose();
    }
}

[UsedImplicitly]
public class RenderingModule : IRenderingModule
{
    internal IWindow RootWindow = null!;
    private List<Backstage> _backstages = [];
    internal LogRenderer LogRenderer = null!;
    internal TextRenderer TextRenderer = null!;
    
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;

    private ITexture _renderTarget = null!;
    private ITexture _renderDepth = null!;
    private ITextureView _renderTargetView = null!;
    private ITextureView _renderDepthView = null!;

    private float BaseResolutionScale => (float)RootWindow.Size.X / RootWindow.FramebufferSize.X;
    private float ResolutionScale => BaseResolutionScale * 1.0f;

    private Vector2D<int> FramebufferSize => new(
        (int)Math.Round(RootWindow.FramebufferSize.X * ResolutionScale),
        (int)Math.Round(RootWindow.FramebufferSize.Y * ResolutionScale)
    );

    private static int MsaaSamples => 8;

    public void Register(Backstage backstage)
    {
        _backstages.Add(backstage);
    }

    public void Unregister(Backstage backstage)
    {
        _backstages.Remove(backstage);
    }

    private IBuffer _viewMatrixBuffer = null!;
    private IBuffer _objectIndexBuffer = null!;
    private InfiniteInstanceBuffer _infiniteInstanceBuffer = null!;
    
    public void HotInitialize(RenderingResources resources, IWindow window)
    {
        RootWindow = window;
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
        
        Texture.Context = renderContext;
        StaticMesh.Context = renderContext;
        Material.Context = renderContext;
        MaterialInstance.Context = renderContext;
        PipelineBuilder.Context = renderContext;
        InfiniteInstanceBuffer.Context = renderContext;
        RenderContext.Current = renderContext;
        
        LogRenderer = new LogRenderer(this);
        TextRenderer = new TextRenderer();

        CreateRenderTargets();

        window.Render += RenderSingleFrameSync;
        window.FramebufferResize += OnFramebufferResize;
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
    
    private void DisposeRenderTargets()
    {
        // _renderTargetView?.Dispose();
        // _renderDepthView?.Dispose();
        _renderTarget?.Dispose();
        _renderDepth?.Dispose();
    }

    private int _atomsToRenderCount;
    private IRenderable[] _atomsToRender = [];

    private void RenderSingleFrameSync(double deltaTime)
    {
        RenderSingleFrame(deltaTime).GetAwaiter().GetResult();
    }

    [Profile]
    private async Task RenderSingleFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        
        Camera? activeCamera = null;
        foreach (var backstage in _backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
        }
        
        if (activeCamera == null)
        {
            Logger.Error("No active camera found for rendering.");
            return;
        }
        
        activeCamera.UpdateFrustumPlanes();
        
        // var rtv = _swapChain.GetCurrentBackBufferRTV();
        // var dsv = _swapChain.GetDepthBufferDSV();

        var wvpMatrix = activeCamera.AsCameraView().ToMatrix();
        var mapUniformBuffer = _immediateContext.MapBuffer<MatrixFloat>(_viewMatrixBuffer, MapType.Write, MapFlags.Discard);
        mapUniformBuffer[0] = wvpMatrix.Downgrade();
        _immediateContext.UnmapBuffer(_viewMatrixBuffer, MapType.Write);
        
        _immediateContext.SetRenderTargets([_renderTargetView], _renderDepthView, ResourceStateTransitionMode.Transition);
        _immediateContext.ClearRenderTarget(_renderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        _immediateContext.ClearDepthStencil(_renderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        
        _infiniteInstanceBuffer.FrameStart();
        
        LogRenderer.RenderFrame(deltaTime);
        foreach (var backstage in _backstages)
        {
            // Collect all IRenderable entities reachable from the atom tree
            CollectAtomsToRender(activeCamera, ref _atomsToRender, ref _atomsToRenderCount, backstage);
            // Render surviving atoms
            RenderAtomTree(_atomsToRender, _atomsToRenderCount);
        
            _atomsToRenderCount = 0;
            Array.Clear(_atomsToRender);
        }

        TextRenderer.Flush();
        
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
        
        stopwatch.StopAndReport(typeof(RenderingModule), ProfilingContext.RenderingPrepare);
        _swapChain.Present(0);
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

    private void RenderAtomTree(IRenderable[] atomsToRender, int atomsToRenderCount)
    {
        // var newAtomsList = new List<IRenderable>(atomsToRenderCount);

        var preparedRequests = atomsToRender
            .Take(atomsToRenderCount)
            .Select(renderable => renderable.Render())
            .GroupBy(r => new
            {
                r.Mesh,
                r.Material,
                r.RenderScript
            })
            .Select(g => new RenderRequest
            {
                Mesh = g.Key.Mesh,
                Material = g.Key.Material,
                RenderScript = g.Key.RenderScript,
                InstanceCount = g.Sum(renderable => renderable.InstanceCount),
                InstanceTransforms = g.SelectMany(req => req.InstanceTransforms).ToArray(),
                MaterialInstances = g.SelectMany(req => req.MaterialInstances).ToArray(),
            })
            .ToList();

        preparedRequests
            .ForEach(req =>
            {
                req.RenderScript.Render(_immediateContext, req.InstanceCount, req.Mesh, req.InstanceTransforms, req.Material,
                    req.MaterialInstances);
            });
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

    public void ToggleLogRendering() => LogRenderer.OnToggleMode();

    private GameplayContext GameplayContext { get; set; } = GameplayContext.Editor;
    public void SetGameplayContext(GameplayContext context)
    {
        GameplayContext = context;
    } 

    public void HotShutdown()
    {
        TextRenderer.Dispose();
        _backstages = [];
        RootWindow.Render -= RenderSingleFrameSync;
        RootWindow.FramebufferResize -= OnFramebufferResize;
        _atomsToRender = [];
        _viewMatrixBuffer.Dispose();
        _objectIndexBuffer.Dispose();
        _infiniteInstanceBuffer.Dispose();
        DisposeRenderTargets();
    }
}
