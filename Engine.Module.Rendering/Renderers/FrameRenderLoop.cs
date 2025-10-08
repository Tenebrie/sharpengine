using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Multithreading;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Computers;
using Engine.Module.Rendering.Renderers.Atoms;
using Engine.Module.Rendering.Renderers.Debug;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Renderers.Lamina;
using Engine.Module.Rendering.Utilities;
using Silk.NET.Maths;

namespace Engine.Module.Rendering.Renderers;

public class FrameRenderLoop(RenderingHost host, TextRenderer immediateTextRenderer) : IDisposable
{
    private List<Backstage> Backstages => host.Backstages;
    private GameplayContext GameplayContext => host.Hypervisor.GameplayContext;
    
    // Computers
    private readonly CullingComputer _cullingComputer = new(host);
    
    // Renderers
    private readonly DebugFramerateFrameRenderer _debugFramerateFrameRenderer = new(host, immediateTextRenderer);
    private readonly DebugLogFrameRenderer _debugLogFrameRenderer = new(host, immediateTextRenderer);
    private readonly DebugProfilerFrameRenderer _debugProfilerFrameRenderer = new(host, immediateTextRenderer);
    private readonly LaminaRenderer _laminaRenderer = new(host, ImmediateContext);
    private readonly SceneRenderer _sceneRenderer = new(host);
    
    private static IDeviceContext ImmediateContext => RenderContext.Current.ImmediateContext;
    private static IDeviceContext[] DeferredContexts => RenderContext.Current.DeferredContexts;
    private static ISwapChain SwapChain => RenderContext.Current.SwapChain;

    private ITexture RenderTarget => host.RenderTarget;
    private ITextureView RenderTargetView => host.RenderTargetView;
    private ITextureView RenderDepthView => host.RenderDepthView;

    private Vector2D<int> FramebufferSize => host.Hypervisor.Window.GetScaledFramebufferSize();

    public async Task RenderSingleFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingGpuWait);
        
        stopwatch = Profiler.Start();
        RenderStats.Reset();
        FrameCounter.Increment();
        ImmediateContext.ClearStats();
        
        await PrepareRenderers();
        
        ImmediateContext.ClearRenderTarget(RenderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        ImmediateContext.ClearDepthStencil(RenderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        ImmediateContext.SetRenderTargets([RenderTargetView], RenderDepthView, ResourceStateTransitionMode.Transition);
        
        await InvokeLaminaRenderers(deltaTime);
        
        ImmediateContext.SetRenderTargets([RenderTargetView], RenderDepthView, ResourceStateTransitionMode.None);
        var ctx = RenderContext.Current;
        ctx.RenderTargetSize = new Vector2(SwapChain.GetDesc().Width, SwapChain.GetDesc().Height);
        RenderContext.Current = ctx;
        
        await InvokeSceneRenderers(deltaTime);
        
        var rtv = SwapChain.GetCurrentBackBufferRTV();
        var rtvTexture = rtv.GetTexture();
        
        ImmediateContext.ResolveTextureSubresource(
            RenderTarget,
            rtvTexture,
            new ResolveTextureSubresourceAttribs
            {
                Format = SwapChain.GetDesc().ColorBufferFormat,
                SrcTextureTransitionMode = ResourceStateTransitionMode.Transition,
                DstTextureTransitionMode = ResourceStateTransitionMode.Transition,
            }
        );
        
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingFullFrame);
        SwapChain.Present(0);
    }
    
    private int _atomsToRenderCount;
    private IRenderable[] _atomsToRender = [];
    private int _widgetsToRenderCount;
    private ILaminaRenderable[] _widgetsToRender = [];

    private Task PrepareRenderers()
    {
        Camera? activeCamera = null;
        foreach (var backstage in Backstages)
        {
            if (activeCamera != null)
                continue;
            activeCamera = FindActiveCamera(backstage);
        }
        
        Camera.Plane[] frustumPlanes = [];
        var wvpMatrix = Matrix.Identity;
        if (activeCamera != null)
        {
            frustumPlanes = activeCamera.UpdateFrustumPlanes();
            wvpMatrix = activeCamera.AsCameraView().ToMatrix();
        }
        
        var mapUniformBuffer = ImmediateContext.MapBuffer<Vector4Float>(host.ViewMatrixBuffer, MapType.Write, MapFlags.Discard);
        mapUniformBuffer[0] = wvpMatrix[0].Downgrade();
        mapUniformBuffer[1] = wvpMatrix[1].Downgrade();
        mapUniformBuffer[2] = wvpMatrix[2].Downgrade();
        mapUniformBuffer[3] = wvpMatrix[3].Downgrade();
        mapUniformBuffer[4] = new Vector4Float(FramebufferSize.X, FramebufferSize.Y, 1.0f / FramebufferSize.X, 1.0f / FramebufferSize.Y);
        ImmediateContext.UnmapBuffer(host.ViewMatrixBuffer, MapType.Write);
        
        host.InfiniteInstanceBuffer.FrameStart();
        _cullingComputer.ReadResultsAndPrepare();
        if (activeCamera == null)
            return Task.CompletedTask;
        
        foreach (var backstage in Backstages)
        {
            // Collect all IRenderable entities reachable from the atom tree
            var stopwatch = Profiler.Start();
            CollectAtomsToRender(backstage);
            stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCollectAtoms);
        }
        _cullingComputer.SubmitCurrentQueue(frustumPlanes);
        return Task.CompletedTask;
    }
    
    private Task InvokeLaminaRenderers(double _)
    {
        _laminaRenderer.RenderRetainedTexturesWithTiming(_widgetsToRender, _widgetsToRenderCount);
        _widgetsToRenderCount = 0;
        Array.Clear(_widgetsToRender);
        return Task.CompletedTask;
    }

    private Task InvokeSceneRenderers(double deltaTime)
    {
        _sceneRenderer.RenderAtomTree(_atomsToRender, _atomsToRenderCount);
        _atomsToRenderCount = 0;
        Array.Clear(_atomsToRender);
        
        _debugFramerateFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugLogFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugProfilerFrameRenderer.RenderFrameWithTiming(deltaTime);

        immediateTextRenderer.Flush();
        return Task.CompletedTask;
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

    private void CollectAtomsToRender(Atom target)
    {
        if (!Atom.IsValid(target))
            return;

        var instanceCount = GetInstanceCount(target);
        
        if (target is ICullable { CullingEnabled: true } cullable)
        {
            _cullingComputer.QueueForCulling(cullable);
            if (!_cullingComputer.IsVisible(cullable))
            {
                RenderStats.InstancesCulled += instanceCount;
                return;
            }
        }

        if (target is IRenderable renderable)
        {
            // Resize the array if necessary
            if (_atomsToRenderCount >= _atomsToRender.Length)
                Array.Resize(ref _atomsToRender, Math.Max(_atomsToRenderCount + 1, _atomsToRender.Length * 2));
            
            RenderStats.InstancesDrawn += instanceCount;
            _atomsToRender[_atomsToRenderCount++] = renderable;
        }
        if (target is ILaminaRenderable { Dirty: true } laminaRenderable)
        {
            if (_widgetsToRenderCount >= _widgetsToRender.Length)
                Array.Resize(ref _widgetsToRender, Math.Max(_widgetsToRenderCount + 1, _widgetsToRender.Length * 2));
            
            _widgetsToRender[_widgetsToRenderCount++] = laminaRenderable;
        }

        foreach (var child in target.Children)
        {
            CollectAtomsToRender(child);
        }
    }
    
    public void ToggleLogRendering() => _debugLogFrameRenderer.OnToggleMode();

    private static int GetInstanceCount(Atom target)
    {
        return target is IRenderable renderable ? renderable.ProduceRenderRequest().InstanceCount : 1;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        immediateTextRenderer.Dispose();
        _laminaRenderer.Dispose();
    }
}