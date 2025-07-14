using Engine.Rendering.Bgfx;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;
using Engine.Worlds.Entities.BuiltIns;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using Transform = Engine.Core.Common.Transform;

namespace Engine.Rendering;

public unsafe class RenderingCore
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
        
        window.Render += RenderSingleFrame;
        window.Resize += OnResize;
        window.Closing += OnShutdown;
    }
    
    private readonly List<double> _frameTimes = [];

    private void RenderSingleFrame(double delta)
    {
        SetViewRect(0, 0, 0,
            _rootWindow.FramebufferSize.X,
            _rootWindow.FramebufferSize.Y);
        
        Touch(0);
        
        DebugTextClear();
        SetViewClear(0, ClearFlags.Color | ClearFlags.Depth, 0x303030ff, 0, 0);
        
        _frameTimes.Add(delta);
        if (_frameTimes.Count > 100)
            _frameTimes.RemoveAt(0);
        var averageFrameTime = _frameTimes.Count > 0 ? _frameTimes.Average() : 0.0;
        var framerate = 1.0f / averageFrameTime;

        foreach (var backstage in _backstages)
        {
            var camera = FindActiveCamera(backstage);
            var cameraTransform = camera?.Transform.Negate() ?? new Transform
            {
                Position = new Vector(0, 0, 0),
                Rotation = new Quaternion(0, 0, 0, 1),
                Scale = new Vector(1, 1, 1)
            };
            RenderAtomTree(cameraTransform, backstage);
        }
        
        DebugTextWrite(0, 0, DebugColor.White, DebugColor.Blue, "Let there be cube!");
        DebugTextWrite(0, 1, "FPS: " + Math.Round(framerate));
        
        Frame(false);
    }

    private static Camera? FindActiveCamera(Atom target)
    {
        if (target is Camera camera)
        {
            return camera;
        }

        return target.Children.Select(FindActiveCamera).OfType<Camera>().FirstOrDefault();
    }

    private void RenderAtomTree(Transform cameraTransform, Atom target, int depth = 0)
    {
        switch (target)
        {
            case DebugLogoComponent component:
                RenderDebugLogoComponent(component);
                break;
            case StaticMeshComponent staticMesh:
                RenderStaticMeshComponent(cameraTransform, staticMesh);
                break;
        }

        foreach (var child in target.Children)
        {
            RenderAtomTree(cameraTransform, child, depth + 1);
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

    private void RenderStaticMeshComponent(Transform cameraTransform, StaticMeshComponent staticMesh)
    {
        staticMesh.Mesh.Render(cameraTransform, staticMesh.WorldTransform, 0, _rootWindow.FramebufferSize.X, _rootWindow.FramebufferSize.Y, staticMesh.Material);
    }

    private void OnResize(Vector2D<int> size)
    {
        var width = _rootWindow.FramebufferSize.X; 
        var height = _rootWindow.FramebufferSize.Y;

        if (width == 0 || height == 0)
            return;

        Reset(width, height, ResetFlags.Vsync, TextureFormat.Count);
        SetViewRect(0, 0, 0, width, height);
    }

    private static void OnShutdown()
    {
        frame(false);
        shutdown();
    }
}