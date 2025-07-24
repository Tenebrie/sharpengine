using System.Buffers;
using System.Runtime.InteropServices;
using Engine.Core.Enum;
using Engine.Core.Profiling;
using Engine.Rendering.Bgfx;
using Engine.Rendering.Renderers;
using Engine.User.Contracts;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Rendering;

static class ObjCRuntime
{
    private const string LIBOBJC = "/usr/lib/libobjc.A.dylib";

    [DllImport(LIBOBJC, EntryPoint = "sel_registerName")]
    public static extern IntPtr sel_registerName(string selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_getClass")]
    public static extern IntPtr objc_getClass(string name);

    // Generic message-send that returns an object pointer (e.g. [CAMetalLayer layer])
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    // Message‑send for methods that take an object pointer and return void (e.g. setLayer:)
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_Void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    // Message‑send for methods that take a bool/BOOL and return void (e.g. setWantsLayer:)
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_Void_Bool(IntPtr receiver, IntPtr selector, bool arg);

    public static IntPtr GetContentView(IntPtr nsWindowPtr)
    {
        var selContentView = sel_registerName("contentView");
        return objc_msgSend_IntPtr(nsWindowPtr, selContentView);
    }

    public static IntPtr CreateMetalLayer()
    {
        var cls = objc_getClass("CAMetalLayer");
        var selLayer = sel_registerName("layer");
        return objc_msgSend_IntPtr(cls, selLayer);
    }

    public static void AttachLayerToView(IntPtr viewPtr, IntPtr layerPtr)
    {
        var selSetLayer    = sel_registerName("setLayer:");
        var selWantsLayer = sel_registerName("setWantsLayer:");

        objc_msgSend_Void_IntPtr(viewPtr, selSetLayer, layerPtr);
        objc_msgSend_Void_Bool   (viewPtr, selWantsLayer, true);
    }
}

[UsedImplicitly]
public unsafe class RenderingCore : IRendererContract
{
    private IWindow _rootWindow = null!;
    private readonly List<Backstage> _backstages = [];
    private LogRenderer _logRenderer = null!;

    private float BaseResolutionScale => (float)_rootWindow.Size.X / _rootWindow.FramebufferSize.X;
    private float ResolutionScale => BaseResolutionScale * 1.0f;
    internal Vector2D<int> FramebufferSize => new(
        (int)Math.Round(_rootWindow.FramebufferSize.X * ResolutionScale),
        (int)Math.Round(_rootWindow.FramebufferSize.Y * ResolutionScale)
    );

    public void Register(Backstage backstage)
    {
        _backstages.Add(backstage);
    }
    
    public void Unregister(Backstage backstage)
    {
        _backstages.Remove(backstage);
    }

    public void Initialize(IWindow window)
    {
        _rootWindow = window;
        BgfxCallbacks.Install();
        Init initData = default;
        init_ctor(&initData);
        if (OperatingSystem.IsWindows())
        {
            initData.type = RendererType.Direct3D11;
            initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
        }
        else if (OperatingSystem.IsMacOS())
        {
            var nsWindow = window.Native?.Cocoa
                           ?? throw new InvalidOperationException("No Cocoa window!");
            var contentView = ObjCRuntime.GetContentView(nsWindow);
            var metalLayer = ObjCRuntime.CreateMetalLayer();
            ObjCRuntime.AttachLayerToView(contentView, metalLayer);
            
            initData.type = RendererType.Metal;
            initData.platformData.nwh = metalLayer.ToPointer();
        }
        else
        {
            throw new NotSupportedException("Unsupported platform for bgfx initialization.");
        }

        initData.resolution.width = (uint)FramebufferSize.X;
        initData.resolution.height = (uint)FramebufferSize.Y;
        initData.resolution.format = TextureFormat.RGBA8;
        initData.resolution.reset = (uint)ResetFlags.Vsync;
        initData.resolution.numBackBuffers = 2;
        initData.resolution.maxFrameLatency = 3;
        initData.callback = BgfxCallbacks.InterfacePtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        
        HotInitialize(window);
    }

    private ResetFlags _resetFlags = ResetFlags.MsaaX8 | ResetFlags.Maxanisotropy | ResetFlags.Vsync;
    private DebugFlags _debugFlags = DebugFlags.Text;

    private ResetFlags GetResetFlags()
    {
        if (OperatingSystem.IsMacOS())
            return _resetFlags & ~(ResetFlags.MsaaX2 | ResetFlags.MsaaX4 | ResetFlags.MsaaX8);
        return _resetFlags;
    }

    public void HotInitialize(IWindow window)
    {
        _rootWindow = window;
        SetDebug(_debugFlags);
        SetViewClear(ViewId.World, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 0, 0);

        Reset(FramebufferSize.X, FramebufferSize.Y, GetResetFlags(), TextureFormat.Count);
        
        _logRenderer = new LogRenderer(this);
        
        window.Render += RenderSingleFrame;
        window.Resize += OnResize;
        window.Closing += Shutdown;
    }

    static RenderingCore()
    {
        SingleComponentTransforms = new Transform[1];
    }

    [Profile]
    private void RenderSingleFrame(double deltaTime)
    {
        DebugTextClear();
        _logRenderer.RenderFrame(deltaTime);
        
        SetViewRect(ViewId.World, 0, 0,
            FramebufferSize.X,
            FramebufferSize.Y);
        SetViewClear(ViewId.World, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 1.0f, 0);
        Touch(ViewId.World);
    
        foreach (var backstage in _backstages)
        {
            RenderCamera(ViewId.World, FindActiveCamera(backstage));
            RenderAtomTree(backstage);
        }
    
        Frame(false);
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

    private static void RenderCamera(ViewId viewId, Camera? camera)
    {
        if (camera is null)
            return;
        
        Span<float> projMatrix = stackalloc float[16];
        var cameraView = camera.AsCameraView(projMatrix);
        
        fixed (float* viewPtr = cameraView.ViewMatrix)
        fixed (float* projPtr = cameraView.ProjMatrix)
        {
            set_view_transform((ushort)viewId, viewPtr, projPtr);
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
        if (!staticMesh.Mesh.IsValid)
            return;
        SingleComponentTransforms[0] = staticMesh.WorldTransform;
        staticMesh.Mesh.Render(1, SingleComponentTransforms, 0, staticMesh.Material);
    }
    
    private static void RenderTerrainMeshComponent(TerrainMeshComponent staticMesh)
    {
        if (!staticMesh.Mesh.IsValid)
            return;
        SingleComponentTransforms[0] = staticMesh.WorldTransform;
        staticMesh.Mesh.Render(1, SingleComponentTransforms, 0, staticMesh.Material);
    }

    private static void RenderInstancedActorComponent(IInstancedActorComponent instancedActorComponent)
    {
        if (!instancedActorComponent.Mesh.IsValid)
            return;
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
        var width = FramebufferSize.X; 
        var height = FramebufferSize.Y;

        if (width == 0 || height == 0)
            return;

        Reset(width, height, GetResetFlags(), TextureFormat.Count);
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
        Reset(FramebufferSize.X, FramebufferSize.Y, GetResetFlags(), TextureFormat.Count);
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

    public void ToggleLogRendering() => _logRenderer.OnToggleMode(); 

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
