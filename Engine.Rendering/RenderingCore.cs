using System.Drawing;
using System.Numerics;
using Engine.Worlds;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;
using Silk.NET.Maths;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Silk.NET.Windowing;

namespace Engine.Rendering.Bgfx;

public unsafe class BgfxCore
{
    private static IWindow _rootWindow = null!;
    
    public static void Init(IWindow window, WindowOptions opts)
    {
        _rootWindow = window;
        BgfxDebug.Hook();
        Init initData = default;
        init_ctor(&initData);
        initData.type = RendererType.Count;
        initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
        initData.resolution.width = (uint)opts.Size.X;
        initData.resolution.height = (uint)opts.Size.Y;
        initData.resolution.format = TextureFormat.RGBA8;
        initData.callback = BgfxDebug.CallbackPtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        SetDebug(DebugFlags.Text);
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);
    }
    
    private static readonly List<double> FrameTimes = [];

    public static void RenderSingleFrame(double delta, Backstage backstage)
    {
        const int logoSizeX = 40;
        const int logoSizeY = 12;

        SetViewRect(0, 0, 0,
            _rootWindow.FramebufferSize.X,
            _rootWindow.FramebufferSize.Y);
        
        Touch(0);
        
        DebugTextClear();
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);
        
        FrameTimes.Add(delta);
        if (FrameTimes.Count > 100)
            FrameTimes.RemoveAt(0);
        var averageFrameTime = FrameTimes.Count > 0 ? FrameTimes.Average() : 0.0;
        var framerate = 1.0f / averageFrameTime;
        DebugTextWrite(1, 10, "FPS: " + Math.Round(framerate));

        RenderAtomTree(delta, backstage);
        
        DebugTextWrite(1, 1, DebugColor.White, DebugColor.Blue, "Hello world!");
        DebugTextWrite(1, 2, DebugColor.White, DebugColor.Cyan, "This is BGFX debug text that do be debugging");
        DebugTextWrite(1, 5, _rootWindow.FramebufferSize.X + " " + _rootWindow.FramebufferSize.Y);
        
        Frame(false);
    }
    
    public static void RenderAtomTree(double delta, Atom target, int depth = 0)
    {
        if (target is DebugLogoComponent component)
        {
            RenderDebugLogoComponent(component);
        }

        foreach (var child in target.Children)
        {
            RenderAtomTree(delta, child, depth + 1);
        }
    }

    private static void RenderDebugLogoComponent(DebugLogoComponent logoComponent)
    {
        DebugTextImage(
            (int)Math.Round(logoComponent.Actor.Transform.Position.X / 8),
            (int)Math.Round(logoComponent.Actor.Transform.Position.Y / 16),
            DebugLogoComponent.LogoSizeX,
            DebugLogoComponent.LogoSizeY,
            Logo.Bytes,
            160
        );
    }
    
    public static void Resize(Vector2D<int> size)
    {
        var width = _rootWindow.FramebufferSize.X; 
        var height = _rootWindow.FramebufferSize.Y;

        if (width == 0 || height == 0)
            return;

        Reset(width, height, ResetFlags.Vsync, TextureFormat.Count);
        SetViewRect(0, 0, 0, width, height);
    }
    
    public static void Shutdown()
    {
        frame(false);
        shutdown();
    }
}