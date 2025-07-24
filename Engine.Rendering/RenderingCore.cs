using System.Buffers;
using System.Runtime.InteropServices;
using Engine.Core.Enum;
using Engine.Core.Profiling;
using Engine.Rendering.Bgfx;
using Engine.Rendering.Renderers;
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

static class ObjCRuntime
{
    const string LIBOBJC = "/usr/lib/libobjc.A.dylib";
    const string LIBQUARTZ = "/System/Library/Frameworks/QuartzCore.framework/QuartzCore";

    [DllImport(LIBOBJC)]
    public static extern IntPtr objc_getClass(string name);

    [DllImport(LIBOBJC)]
    public static extern IntPtr sel_registerName(string selName);

    // NOTE: The signature of objc_msgSend is varargs; we pick the overload we need
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_SetLayer(IntPtr receiver, IntPtr selector, IntPtr layer);

    [DllImport(LIBQUARTZ)]
    public static extern IntPtr CAMetalLayer_class();  // We'll use this to get the CAMetalLayer class

    [DllImport(LIBOBJC)]
    public static extern IntPtr objc_msgSend_alloc_init(IntPtr cls, IntPtr initSel);
}

static class MetalViewSetup
{
    // selectors we’ll need:
    static readonly IntPtr sel_contentView    = ObjCRuntime.sel_registerName("contentView");
    static readonly IntPtr sel_wantsLayer     = ObjCRuntime.sel_registerName("setWantsLayer:");
    static readonly IntPtr sel_setLayer       = ObjCRuntime.sel_registerName("setLayer:");
    static readonly IntPtr sel_alloc          = ObjCRuntime.sel_registerName("alloc");
    static readonly IntPtr sel_init           = ObjCRuntime.sel_registerName("init");

    public static IntPtr GetContentView(IntPtr nsWindow)
    {
        return ObjCRuntime.objc_msgSend_IntPtr(nsWindow, sel_contentView);
    }

    public static void EnableLayerBacking(IntPtr nsView)
    {
        ObjCRuntime.objc_msgSend_bool(nsView, sel_wantsLayer, true);
    }

    public static IntPtr CreateMetalLayer()
    {
        // CAMetalLayer is in QuartzCore, but objc_getClass will find it
        var cls = ObjCRuntime.objc_getClass("CAMetalLayer");
        var alloc = ObjCRuntime.objc_msgSend_IntPtr(cls, sel_alloc);
        return ObjCRuntime.objc_msgSend_IntPtr(alloc, sel_init);
    }

    public static void AttachLayer(IntPtr nsView, IntPtr metalLayer)
    {
        ObjCRuntime.objc_msgSend_SetLayer(nsView, sel_setLayer, metalLayer);
    }
}

public unsafe class RenderingCore : IRendererContract
{
    internal IWindow RootWindow = null!;
    internal readonly List<Backstage> Backstages = [];
    internal LogRenderer LogRenderer = null!;

    public void Register(Backstage backstage)
    {
        Backstages.Add(backstage);
    }
    
    public void Unregister(Backstage backstage)
    {
        Backstages.Remove(backstage);
    }

    public void Initialize(IWindow window, WindowOptions opts)
    {
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
            initData.type = RendererType.Metal;
            var cocoaWindowPtr = window.Native!.Cocoa!.Value;  // NSWindow*
            var viewPtr = MetalViewSetup.GetContentView(cocoaWindowPtr);
            MetalViewSetup.EnableLayerBacking(viewPtr);
            var metalLayer = MetalViewSetup.CreateMetalLayer();
            MetalViewSetup.AttachLayer(viewPtr, metalLayer);
            initData.platformData.nwh = metalLayer.ToPointer();
        }
        else
        {
            throw new NotSupportedException("Unsupported platform for bgfx initialization.");
        }

        initData.resolution.width = (uint)opts.Size.X;
        initData.resolution.height = (uint)opts.Size.Y;
        initData.resolution.format = TextureFormat.RGBA8;
        initData.resolution.reset = (uint)ResetFlags.Vsync;
        initData.resolution.numBackBuffers = 2;
        initData.resolution.maxFrameLatency = 3;
        initData.callback = BgfxCallbacks.InterfacePtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        
        HotInitialize(window, opts);
    }

    private ResetFlags _resetFlags = ResetFlags.MsaaX8 | ResetFlags.Maxanisotropy | ResetFlags.Vsync;
    private DebugFlags _debugFlags = DebugFlags.Text;

    public void HotInitialize(IWindow window, WindowOptions opts)
    {
        RootWindow = window;
        SetDebug(_debugFlags);
        SetViewClear(0, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 0, 0);

        Reset(opts.Size.X, opts.Size.Y, _resetFlags, TextureFormat.Count);
        
        LogRenderer = new LogRenderer(this);
        
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
        SetViewRect(0, 0, 0,
            RootWindow.FramebufferSize.X,
            RootWindow.FramebufferSize.Y);
        
        Touch(0);
        
        DebugTextClear();
        SetViewClear(0, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 1.0f, 0);
        
        LogRenderer.RenderFrame(deltaTime);
        
        foreach (var backstage in Backstages)
        {
            RenderCamera(0, FindActiveCamera(backstage));
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
        var width = RootWindow.FramebufferSize.X; 
        var height = RootWindow.FramebufferSize.Y;

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
        Reset(RootWindow.FramebufferSize.X, RootWindow.FramebufferSize.Y, _resetFlags, TextureFormat.Count);
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

    public void ToggleLogRendering() => LogRenderer.OnToggleMode(); 

    private GameplayContext GameplayContext { get; set; } = GameplayContext.Editor;
    public void SetGameplayContext(GameplayContext context)
    {
        GameplayContext = context;
    }

    public void DisconnectCallbacks()
    {
        RootWindow.Render -= RenderSingleFrame;
        RootWindow.Resize -= OnResize;
        RootWindow.Closing -= Shutdown; 
    }

    public void Shutdown() 
    {
        DisconnectCallbacks();

        frame(false);
        shutdown();
    }
}
