using System.Drawing;
using System.Numerics;
using Engine.Worlds;
using Silk.NET.Maths;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Silk.NET.Windowing;

namespace Engine.Rendering.Bgfx;

public unsafe class BgfxCore
{
    private static IWindow _rootWindow = null!;
    private static Vector2 _logoVelocity = new (143, 93);
    private static Vector2 _logoPosition = Vector2.Zero;
    private static int _logoBumpCount = 0;
    private static float _logoAcceleration = 0.1f;
    
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
        initData.resolution.reset = (uint)ResetFlags.Vsync;
        initData.callback = BgfxDebug.CallbackPtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        SetDebug(DebugFlags.Text);
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);

        const int logoSizeX = 40;
        const int logoSizeY = 12;
        _logoPosition = new Vector2(opts.Size.X / 8.0f, opts.Size.Y / 16.0f) / 2 - new Vector2(logoSizeX, logoSizeY) / 2;
    }

    public static void RenderSingleFrame(double delta, World world)
    {
        const int logoSizeX = 40;
        const int logoSizeY = 12;

        var cellCountX = (int)Math.Floor(_rootWindow.FramebufferSize.X / 8.0f);
        var cellCountY = (int)Math.Floor(_rootWindow.FramebufferSize.Y / 16.0f);
        
        _logoPosition += _logoVelocity * (float)delta;
        _logoVelocity = new Vector2(Math.Sign(_logoVelocity.X), Math.Sign(_logoVelocity.Y));

        if (_logoPosition.X + logoSizeX * 8 >= _rootWindow.FramebufferSize.X)
        {
            _logoVelocity.X = -1;
            _logoPosition.X = _rootWindow.FramebufferSize.X - logoSizeX * 8;
            _logoBumpCount++;
        }
        if (_logoPosition.X < 0)
        {
            _logoVelocity.X = 1;
            _logoPosition.X = 0;
            _logoBumpCount++;
        }

        if (_logoPosition.Y + logoSizeY * 16 >= _rootWindow.FramebufferSize.Y)
        {
            _logoVelocity.Y = -1;
            _logoPosition.Y = _rootWindow.FramebufferSize.Y - logoSizeY * 16;
            _logoBumpCount++;
        }
        if (_logoPosition.Y < 0)
        {
            _logoVelocity.Y = 1;
            _logoPosition.Y = 0;
            _logoBumpCount++;
        }

        _logoVelocity = new Vector2(_logoVelocity.X * 143, _logoVelocity.Y * 93) * (1 + _logoAcceleration * _logoBumpCount);
        
        SetViewRect(0, 0, 0,
            _rootWindow.FramebufferSize.X,
            _rootWindow.FramebufferSize.Y);
        
        Touch(0);
        
        DebugTextClear();
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);

        DebugTextImage(
            (int)Math.Round(_logoPosition.X / 8),
            (int)Math.Round(_logoPosition.Y / 16),
            logoSizeX,
            logoSizeY,
            Logo.Bytes,
            160
        );
        
        DebugTextWrite(1, 1, DebugColor.White, DebugColor.Blue, "Hello world!");
        DebugTextWrite(1, 2, DebugColor.White, DebugColor.Cyan, "This is BGFX debug text that do be debugging");
        DebugTextWrite(1, 5, _rootWindow.FramebufferSize.X + " " + _rootWindow.FramebufferSize.Y);
        DebugTextWrite(1, 6, cellCountX + " " + cellCountY);
        DebugTextWrite(1, 7, Math.Round(_logoPosition.X) + " " + Math.Round(_logoPosition.Y));
        DebugTextWrite(1, 8, "Bump count: " + _logoBumpCount + " " + "Speed multiplier: " + (1 + _logoAcceleration * _logoBumpCount));
        
        Frame(false);
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