using Engine.Codegen.Bgfx.Unsafe;
using static Engine.Codegen.Bgfx.Unsafe.bgfx;
using Silk.NET.Windowing;

namespace Engine.Rendering.Bgfx;

public unsafe class BgfxCore
{
    private static IWindow _rootWindow = null!;
    public static void Init(IWindow window, WindowOptions opts)
    {
        _rootWindow = window;
        BgfxDebug.Hook();
        bgfx.Init init = default;
        bgfx.init_ctor(&init);
        init.type = bgfx.RendererType.Count;
        init.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
        init.resolution.width = (uint)opts.Size.X;
        init.resolution.height = (uint)opts.Size.Y;
        init.resolution.format = bgfx.TextureFormat.RGBA8;
        init.resolution.reset = (uint)bgfx.ResetFlags.Vsync;
        init.callback = BgfxDebug.CallbackPtr;
        
        if (!bgfx.init(&init))
            throw new InvalidOperationException("bgfx init failed");
        SetDebug(DebugFlags.Text);
    }

    public static void Frame(double delta)
    {
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);
        SetViewRect(0, 0, 0,
            _rootWindow.FramebufferSize.X,
            _rootWindow.FramebufferSize.Y);
        touch(0);
        frame(false);
    }
    
    public static void Resize()
    {
        var w = (uint)_rootWindow.FramebufferSize.X; 
        var h = (uint)_rootWindow.FramebufferSize.Y;

        if (w == 0 || h == 0)
            return;

        bgfx.reset(w, h, (uint)bgfx.ResetFlags.Vsync, bgfx.TextureFormat.Count);
        bgfx.set_view_rect(0, 0, 0, (ushort)w, (ushort)h);
    }
    
    public static void Shutdown()
    {
        bgfx.frame(false);
        bgfx.shutdown();
    }
}