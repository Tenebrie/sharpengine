using System.Buffers;
using Engine.Core.Common;
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
        HotInitialize(window, opts);
    }

    private const ResetFlags BgfxResetFlags = ResetFlags.Vsync | ResetFlags.MsaaX8; 

    public void HotInitialize(IWindow window, WindowOptions opts)   
    {
        _rootWindow = window;
        SetDebug(DebugFlags.Text | DebugFlags.Stats | DebugFlags.Profiler);
        SetViewClear(0, (ClearFlags.Color | ClearFlags.Depth), 0x303030ff, 0, 0);

        Reset(opts.Size.X, opts.Size.Y, BgfxResetFlags, TextureFormat.Count);
        
        window.Render += RenderSingleFrame;
        window.Resize += OnResize;
        window.Closing += Shutdown;
    }
    
    private readonly List<double> _frameTimes = [];

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
    }

    private static Camera? FindActiveCamera(Atom target)
    {
        if (target is Camera { ActiveInEditor: true } camera)
        {
            return camera;
        }

        return target.Children.Select(FindActiveCamera).OfType<Camera>().FirstOrDefault();
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
            case DebugLogoComponent component:
                RenderDebugLogoComponent(component);
                break;
            case StaticMeshComponent staticMesh:
                RenderStaticMeshComponent(staticMesh);
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

    private static void RenderStaticMeshComponent(StaticMeshComponent staticMesh)
    {
        Transform[] worldTransforms = [staticMesh.WorldTransform];
        staticMesh.Mesh.Render(1, worldTransforms, 0, staticMesh.Material);
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

        Reset(width, height, ResetFlags.Vsync | ResetFlags.MsaaX8, TextureFormat.Count);
        SetViewRect(0, 0, 0, width, height);
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

/**
// once at start-up
var fstash   = new FontSystem();                     // FontStashSharp
var font     = fstash.AddFont("assets/Roboto-Regular.ttf");
ITexture2D tx = new BgfxTexture2D(fstash.Texture);   // bind atlas as bgfx texture
// every frame
var layout = font.LayoutText("Hello, Maya!", 32f);
foreach (var g in layout)
    WriteQuad(vertexWriter, g.X, g.Y, g.Width, g.Height,
              g.U1, g.V1, g.U2, g.V2);              // into a transient VB
bgfx.setTexture(0, s_tex, tx.Handle);
bgfx.submit(viewId, program);
*/
