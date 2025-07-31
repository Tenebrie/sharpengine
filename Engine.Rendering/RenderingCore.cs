using System.Buffers;
using System.Diagnostics;
using Engine.Assets.Rendering;
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

    private DynamicVertexBufferHandle _instanceTransformVertexBuffer;

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
        initData.resolution.reset = 0;
        initData.resolution.numBackBuffers = 2;
        initData.resolution.maxFrameLatency = 3;
        initData.callback = BgfxCallbacks.InterfacePtr;
        
        if (!init(&initData))
            throw new InvalidOperationException("bgfx init failed");
        
        HotInitialize(window);
    }

    private ResetFlags _resetFlags = ResetFlags.MsaaX8 | ResetFlags.Maxanisotropy;
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
        
        VertexLayout instLayout = default;
        CreateVertexLayout(ref instLayout, [
            new VertexLayoutAttribute(Attrib.TexCoord4, 4, AttribType.Float, false, false),
            new VertexLayoutAttribute(Attrib.TexCoord5, 4, AttribType.Float, false, false),
            new VertexLayoutAttribute(Attrib.TexCoord6, 4, AttribType.Float, false, false),
            new VertexLayoutAttribute(Attrib.TexCoord7, 4, AttribType.Float, false, false),
        ]);

        _instanceTransformVertexBuffer = create_dynamic_vertex_buffer(1, &instLayout, (ushort)BufferFlags.AllowResize);
        
        window.Render += RenderSingleFrame;
        window.Resize += OnResize;
        window.Closing += Shutdown;
    }

    private int _atomsToRenderCount = 0;
    private IRenderable[] _atomsToRender = [];
    private int _culledAtomsCount = 0;
    private IRenderable[] _culledAtoms = [];

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
        
        activeCamera.UpdateFrustumPlanes();
        
        foreach (var backstage in _backstages)
        {
            // Collect all IRenderable entities reachable from the atom tree
            CollectAtomsToRender(ref _atomsToRender, ref _atomsToRenderCount, backstage);
            // Cull entities off-camera
            FrustumCulling(activeCamera, ref _atomsToRender, _atomsToRenderCount, ref _culledAtoms, ref _culledAtomsCount);
            // Render surviving atoms
            RenderAtomTree(_culledAtoms, _culledAtomsCount);
            
            _atomsToRenderCount = 0;
            _culledAtomsCount = 0;
            Array.Clear(_atomsToRender);
            Array.Clear(_culledAtoms);
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
    
    private static void CollectAtomsToRender(ref IRenderable[] entitiesToRender, ref int entitiesToRenderCount, Atom target)
    {
        if (target is IRenderable renderable && Atom.IsValid(target))
        {
            // Resize the array if necessary
            if (entitiesToRenderCount >= entitiesToRender.Length)
                Array.Resize(ref entitiesToRender, Math.Max(entitiesToRenderCount + 1, entitiesToRender.Length * 2));
            
            entitiesToRender[entitiesToRenderCount++] = renderable;
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(ref entitiesToRender, ref entitiesToRenderCount, child);
        }
    }
    
    private static void FrustumCulling(
        Camera activeCamera,
        ref IRenderable[] atomsToRender,
        int atomsToRenderCount,
        ref IRenderable[] culledAtoms,
        ref int culledAtomsCount)
    {
        for (var index = 0; index < atomsToRenderCount; index++)
        {
            var atom = atomsToRender[index];
            atom.PerformCulling(activeCamera);
            if (atom.IsOnScreen)
            {
                // Resize the culledAtoms array if necessary
                if (culledAtomsCount >= culledAtoms.Length)
                    Array.Resize(ref culledAtoms, Math.Max(culledAtomsCount + 1, culledAtoms.Length * 2));
                culledAtoms[culledAtomsCount++] = atom;
            }
        }
    }

    private static uint CollectInstanceCount(IRenderable[] culledAtoms, int culledAtomsCount)
    {
        uint instanceCount = 0;
        for (var index = 0; index < culledAtomsCount; index++)
        {
            var atom = culledAtoms[index];
            instanceCount += (uint)atom.GetInstanceCount();
        }

        return instanceCount;
    }

    private void RenderAtomTree(IRenderable[] atomsToRender, int atomsToRenderCount)
    {
        var instanceCount = CollectInstanceCount(atomsToRender, atomsToRenderCount);
        if (instanceCount == 0)
            return;

        var instanceTransformPrepBuffer = ArrayPool<float>.Shared.Rent((int)instanceCount * 16);
        
        var renderContext = new RenderContext
        {
            ViewId = (ushort)ViewId.World,
            InstanceTransformCount = 0,
            InstanceTransformBuffer = _instanceTransformVertexBuffer,
            InstanceTransformStride = 16,
            InstanceTransformPrepBuffer = instanceTransformPrepBuffer,
        };

        for (var index = 0; index < atomsToRenderCount; index++)
        {
            var renderable = atomsToRender[index];
            renderable.PrepareRender(ref renderContext);
        }

        const ushort bytesPerMatrix = 16 * sizeof(float);
        fixed (float* instanceTransformPtr = renderContext.InstanceTransformPrepBuffer)
        {
            var mem = copy(instanceTransformPtr, instanceCount * bytesPerMatrix);
            update_dynamic_vertex_buffer(_instanceTransformVertexBuffer, 0, mem);
        }
        
        renderContext.InstanceTransformCount = 0;

        for (var index = 0; index < atomsToRenderCount; index++)
        {
            var renderable = atomsToRender[index];
            renderable.Render(ref renderContext);
        }

        ArrayPool<float>.Shared.Return(instanceTransformPrepBuffer);
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
        destroy_dynamic_vertex_buffer(_instanceTransformVertexBuffer);
        _rootWindow.Render -= RenderSingleFrame;
        _rootWindow.Resize -= OnResize;
        _rootWindow.Closing -= Shutdown;
    }

    public void Shutdown() 
    {
        DisconnectCallbacks();

        Frame(false);
        shutdown();
    }
}
