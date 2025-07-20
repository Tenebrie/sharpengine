using System.Buffers;
using Engine.Core.Enum;
using Engine.Core.Profiling;
using Engine.Rendering.Bgfx;
using Engine.User.Contracts;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Rendering;

internal enum ViewId : ushort
{
    Main = 0,
}

public unsafe class RenderingCore : IRendererContract
{
    private IWindow _rootWindow = null!;
    private List<Backstage> _backstages = [];

    public void Register(Backstage backstage)
    {
        _backstages.Add(backstage);
    }
    
    public void Unregister(Backstage backstage)
    {
        _backstages.Remove(backstage);
    }

    public void Initialize(IWindow window, WindowOptions opts)
    {
        BgfxCallbacks.Install();
        Init initData = default;
        init_ctor(&initData);
        initData.type = RendererType.Direct3D12;
        initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
        initData.resolution.width = (uint)opts.Size.X;
        initData.resolution.height = (uint)opts.Size.Y;
        initData.resolution.format = TextureFormat.RGBA8;
        initData.resolution.numBackBuffers = 4;
        initData.resolution.maxFrameLatency = 3;
        initData.callback = BgfxCallbacks.InterfacePtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        HotInitialize(window, opts);
    }

    private ResetFlags _resetFlags = ResetFlags.MsaaX8;
    private DebugFlags _debugFlags = DebugFlags.Text;

    public void HotInitialize(IWindow window, WindowOptions opts)
    {
        _rootWindow = window;
        SetDebug(_debugFlags);
        SetViewClear(0, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 0, 0);

        Reset(opts.Size.X, opts.Size.Y, _resetFlags, TextureFormat.Count);
        
        window.Render += RenderSingleFrame;
        window.Resize += OnResize;
        window.Closing += Shutdown;
    }
    
    private readonly List<double> _frameTimes = [];

    static RenderingCore()
    {
        SingleComponentTransforms = new Transform[1];
    }

    [Profile]
    private void RenderSingleFrame(double delta)
    {
        SetViewRect(0, 0, 0,
            _rootWindow.FramebufferSize.X,
            _rootWindow.FramebufferSize.Y);
        
        Touch(0);
        
        DebugTextClear();
        SetViewClear(0, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 1.0f, 0);
        
        _frameTimes.Add(delta);
        if (_frameTimes.Count > 100)
            _frameTimes.RemoveAt(0);
        var averageFrameTime = _frameTimes.Count > 0 ? _frameTimes.Average() : 0.0;
        var framerate = 1.0f / averageFrameTime;
        
        foreach (var backstage in _backstages)
        {
            RenderCamera(0, FindActiveCamera(backstage));
            RenderAtomTree(backstage);
        }
        
        DebugTextWrite(0, 0, DebugColor.White, DebugColor.Blue, "Let there be cube!");
        DebugTextWrite(0, 1, "FPS: " + Math.Round(framerate));
        
        Frame(false);
        // var st = get_stats();
        // double cpuMs = (st->cpuTimeEnd - st->cpuTimeBegin) * 1000.0 / st->cpuTimerFreq;
        // double gpuMs = (st->gpuTimeEnd - st->gpuTimeBegin) * 1000.0 / st->gpuTimerFreq;
        // Console.WriteLine($"cpu {cpuMs:0.000}  gpu ${gpuMs:0.000} draws {st->numDraw}  pso {st->numPrograms}  waitR {st->waitRender}");
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

    private static void RenderCamera(ushort viewId, Camera? camera)
    {
        if (camera is null)
            return;
        
        Span<float> projMatrix = stackalloc float[16];
        var cameraView = camera.AsCameraView(projMatrix);
        
        fixed (float* viewPtr = cameraView.ViewMatrix)
        fixed (float* projPtr = cameraView.ProjMatrix)
        {
            set_view_transform(viewId, viewPtr, projPtr);
        }
    }

    private void RenderAtomTree(Atom target)
    {
        switch (target)
        {
            case StaticMeshComponent staticMesh:
                RenderStaticMeshComponent(staticMesh);
                break;
            case TerrainMeshComponent staticMesh:
                RenderTerrainMeshComponent(staticMesh);
                break;
            case IInstancedActorComponent instancedActorManager:
                RenderInstancedActorComponent(instancedActorManager);
                break;
        }

        foreach (var child in target.Children)
        {
            RenderAtomTree(child);
        }
    }

    [ThreadStatic] private static readonly Transform[] SingleComponentTransforms;
    private static void RenderStaticMeshComponent(StaticMeshComponent staticMesh)
    {
        SingleComponentTransforms[0] = staticMesh.WorldTransform;
        staticMesh.Mesh.Render(1, SingleComponentTransforms, 0, staticMesh.Material);
    }
    
    private static void RenderTerrainMeshComponent(TerrainMeshComponent staticMesh)
    {
        SingleComponentTransforms[0] = staticMesh.WorldTransform;
        staticMesh.Mesh.Render(1, SingleComponentTransforms, 0, staticMesh.Material);
    }

    private static void RenderInstancedActorComponent(IInstancedActorComponent instancedActorComponent)
    {
        var transforms = ArrayPool<Transform>.Shared.Rent(instancedActorComponent.Instances.Count);
        try
        {
            for (var i = 0; i < instancedActorComponent.Instances.Count; i++)
            {
                var actor = instancedActorComponent.Instances[i];
                transforms[i] = actor.WorldTransform;
            }

            instancedActorComponent.Mesh.Render((uint)instancedActorComponent.Instances.Count, transforms, 0,
                instancedActorComponent.Material);
        }
        finally
        {
            ArrayPool<Transform>.Shared.Return(transforms);
        }
    }

    private void OnResize(Vector2D<int> size)
    {
        var width = _rootWindow.FramebufferSize.X; 
        var height = _rootWindow.FramebufferSize.Y;

        if (width == 0 || height == 0)
            return;

        Reset(width, height, _resetFlags, TextureFormat.Count);
        SetViewRect(0, 0, 0, width, height);
    }
    
    public void ToggleResetFlags(ResetFlags flags)
    {
        if (flags == ResetFlags.None)
            return;

        // Toggle flags
        if ((_resetFlags & flags) == flags)
        {
            _resetFlags &= ~flags;
        }
        else
        {
            _resetFlags |= flags;
        }

        // Reset the renderer with the new flags
        Reset(_rootWindow.FramebufferSize.X, _rootWindow.FramebufferSize.Y, _resetFlags, TextureFormat.Count);
    }
    
    public void ToggleDebugFlags(DebugFlags flags)
    {
        if (flags == DebugFlags.None)
            return;

        // Toggle flags
        if ((_debugFlags & flags) == flags)
        {
            _debugFlags &= ~flags;
        }
        else
        {
            _debugFlags |= flags;
        }

        // Set the new debug flags
        SetDebug(_debugFlags);
    }

    private GameplayContext GameplayContext { get; set; } = GameplayContext.Editor;
    public void SetGameplayContext(GameplayContext context)
    {
        GameplayContext = context;
    }

    public void DisconnectCallbacks()
    {
        _rootWindow.Render -= RenderSingleFrame;
        _rootWindow.Resize -= OnResize;
        _rootWindow.Closing -= Shutdown; 
    }

    public void Shutdown() 
    {
        DisconnectCallbacks();

        frame(false);
        shutdown();
    }
}
