using System.Diagnostics;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Rendering.Bgfx;
using Engine.Rendering.Platforms;
using Engine.Rendering.Renderers;
using Engine.User.Contracts;
using Engine.Worlds.Entities;
using Engine.Worlds.Interfaces;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Rendering;


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
            WindowsRuntime.PrepareInit(ref initData, window);
        else if (OperatingSystem.IsMacOS())
            MacRuntime.PrepareInit(ref initData, window);
        else
            throw new NotSupportedException("Unsupported platform for bgfx initialization.");

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
        
        Camera? activeCamera = null;
        foreach (var backstage in _backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
            RenderCamera(ViewId.World, activeCamera);
        }
        
        if (activeCamera == null)
        {
            Logger.Error("No active camera found for rendering.");
            return;
        }
        
        List<IRenderable> atomsToRender = [];
        List<IRenderable> culledAtoms = [];
        activeCamera.UpdateFrustumPlanes();
    
        foreach (var backstage in _backstages)
        {
            CollectAtomsToRender(ref atomsToRender, backstage);
            FrustumCulling(activeCamera, ref atomsToRender, ref culledAtoms);
            RenderAtomTree(culledAtoms);
            
            atomsToRender.Clear();
            culledAtoms.Clear();
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
    
    private void CollectAtomsToRender(ref List<IRenderable> entitiesToRender, Atom target)
    {
        if (target is IRenderable renderable)
        {
            entitiesToRender.Add(renderable);
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(ref entitiesToRender, child);
        }
    }
    
    private void FrustumCulling(Camera activeCamera, ref List<IRenderable> atomsToRender, ref List<IRenderable> culledAtoms)
    {
        foreach (var atom in atomsToRender)
        {
            atom.PerformCulling(activeCamera);
            if (atom.IsOnScreen)
                culledAtoms.Add(atom);
        }
    }


    private void RenderAtomTree(List<IRenderable> atomsToRender)
    {
        foreach (var renderable in atomsToRender)
        {
            renderable.Render();
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
