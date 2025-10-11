using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules.EntitySystem;
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
    private static ISwapChain SwapChain => RenderContext.Current.SwapChain;

    private ITexture RenderTarget => host.RenderTarget;
    private ITextureView RenderTargetView => host.RenderTargetView;
    private ITextureView RenderDepthView => host.RenderDepthView;

    private Vector2D<int> FramebufferSize => host.Hypervisor.Window.GetScaledFramebufferSize();

    public void RenderSingleFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingGpuWait);
        
        var fullFrameStopwatch = Profiler.Start();

        RenderStats.Reset();
        ImmediateContext.ClearStats();
        
        PrepareRenderers();
        
        ImmediateContext.ClearRenderTarget(RenderTargetView, new Vector4(0.35f, 0.35f, 0.35f, 1.0f), ResourceStateTransitionMode.Transition);
        ImmediateContext.ClearDepthStencil(RenderDepthView, ClearDepthStencilFlags.Depth, 1.0f, 0, ResourceStateTransitionMode.Transition);
        ImmediateContext.SetRenderTargets([RenderTargetView], RenderDepthView, ResourceStateTransitionMode.Transition);
        
        InvokeLaminaRenderers(deltaTime);
        
        ImmediateContext.SetRenderTargets([RenderTargetView], RenderDepthView, ResourceStateTransitionMode.None);
        var ctx = RenderContext.Current;
        ctx.RenderTargetSize = new Vector2(SwapChain.GetDesc().Width, SwapChain.GetDesc().Height);
        RenderContext.Current = ctx;
        
        InvokeSceneRenderers(deltaTime);
        
        stopwatch = Profiler.Start();
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
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingResolveRenderTarget);
        
        stopwatch = Profiler.Start();
        SwapChain.Present(0);
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingPresent);
        fullFrameStopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingTotal);
    }
    
    private int _atomsToRenderCount;
    private RenderableHandle[] _atomsToRender = [];
    private int _widgetsToRenderCount;
    private ILaminaRenderable[] _widgetsToRender = [];

    private void PrepareRenderers()
    {
        var activeCamera = host.RegisteredCameras.FindActive(GameplayContext == GameplayContext.Editor);
        
        ICamera.Plane[] frustumPlanes = [];
        var wvpMatrix = Matrix.Identity;
        if (activeCamera.HasValue)
        {
            frustumPlanes = activeCamera.Value.FrustumPlanes;
            wvpMatrix = activeCamera.Value.InverseWorldTransform.Data;
        }
        
        var mapUniformBuffer = ImmediateContext.MapBuffer<Vector4Float>(host.ViewMatrixBuffer, MapType.Write, MapFlags.Discard);
        mapUniformBuffer[0] = wvpMatrix[0].Downgrade();
        mapUniformBuffer[1] = wvpMatrix[1].Downgrade();
        mapUniformBuffer[2] = wvpMatrix[2].Downgrade();
        mapUniformBuffer[3] = wvpMatrix[3].Downgrade();
        mapUniformBuffer[4] = new Vector4Float(FramebufferSize.X, FramebufferSize.Y, 1.0f / FramebufferSize.X, 1.0f / FramebufferSize.Y);
        ImmediateContext.UnmapBuffer(host.ViewMatrixBuffer, MapType.Write);
        
        host.InfiniteInstanceBuffer.FrameStart();
        var stopwatch = Profiler.Start();
        _cullingComputer.ReadResultsAndPrepare();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCullingComputerRead);
        if (activeCamera == null) return;

        stopwatch = Profiler.Start();
        CollectRegisteredAtomsToRender();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCollectAtoms);
        stopwatch = Profiler.Start();
        _cullingComputer.SubmitCurrentQueue(frustumPlanes);
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCullingComputerWrite);
    }
    
    private void InvokeLaminaRenderers(double _)
    {
        _laminaRenderer.RenderRetainedTexturesWithTiming(_widgetsToRender, _widgetsToRenderCount);
        _widgetsToRenderCount = 0;
        Array.Clear(_widgetsToRender);
    }

    private void InvokeSceneRenderers(double deltaTime)
    {
        _sceneRenderer.RenderAtomTree(_atomsToRender, _atomsToRenderCount);
        _atomsToRenderCount = 0;
        Array.Clear(_atomsToRender);
        
        _debugFramerateFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugLogFrameRenderer.RenderFrameWithTiming(deltaTime);
        _debugProfilerFrameRenderer.RenderFrameWithTiming(deltaTime);

        var stopwatch = Profiler.Start();
        immediateTextRenderer.Flush();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingImmediateTextFlush);
    }

    private void CollectRegisteredAtomsToRender()
    {
        var renderables = host.RegisteredRenderables.AsArray();
        foreach (var handle in renderables.AsSpan())
        {
            var instanceCount = GetInstanceCount(handle);
            
            if (handle.Renderable is ICullable { CullingEnabled: true } cullable)
            {
                _cullingComputer.QueueForCulling(cullable);
                if (!_cullingComputer.IsVisible(cullable))
                {
                    RenderStats.InstancesCulled += instanceCount;
                    continue;
                }
            }
            
            if (_atomsToRenderCount >= _atomsToRender.Length)
                Array.Resize(ref _atomsToRender, Math.Max(_atomsToRenderCount + 1, _atomsToRender.Length * 2));
            
            RenderStats.InstancesDrawn += instanceCount;
            _atomsToRender[_atomsToRenderCount++] = handle;
        }
        
        var laminaHandleList = host.RegisteredLaminaElements.AsArray();
        foreach (var handle in laminaHandleList.AsSpan())
        {
            if (_widgetsToRenderCount >= _widgetsToRender.Length)
                Array.Resize(ref _widgetsToRender, Math.Max(_widgetsToRenderCount + 1, _widgetsToRender.Length * 2));
            _widgetsToRender[_widgetsToRenderCount++] = handle.Renderable;
        }
    }
    
    public void ToggleLogRendering() => _debugLogFrameRenderer.OnToggleMode();

    private static int GetInstanceCount(RenderableHandle handle)
    {
        var request = handle.RenderRequest;
        if (!request.HasValue)
            return 0;
        return request.Value.InstanceCount;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        immediateTextRenderer.Dispose();
        _laminaRenderer.Dispose();
    }
}