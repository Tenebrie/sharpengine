using System.Diagnostics.CodeAnalysis;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Enum;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Renderers;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Renderers.Splash;
using Engine.Module.Rendering.Utilities;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using ResourceDimension = Diligent.ResourceDimension;
using Usage = Diligent.Usage;

namespace Engine.Module.Rendering;

[UsedImplicitly]
public class RenderingHost : IRenderingHost
{
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;

    private FrameRenderLoop _frameRenderLoop = null!;
    
    internal ITexture RenderTarget = null!;
    private ITexture _renderDepth = null!;
    internal ITextureView RenderTargetView = null!;
    internal ITextureView RenderDepthView = null!;
    
    public IRootHypervisor Hypervisor { get; set; } = null!;
    internal IWindow RootWindow => Hypervisor.Window;
    
    internal readonly CameraRegistrationHandler RegisteredCameras = new();
    internal readonly LaminaRegistrationHandler RegisteredLaminaElements = new();
    internal readonly RenderableRegistrationHandler RegisteredRenderables = new();

    private Vector2D<int> FramebufferSize => RootWindow.GetScaledFramebufferSize();

    // Constant camera matrix buffer
    internal IBuffer ViewMatrixBuffer = null!;
    
    // Contains the index of the current instance
    private IBuffer _instanceIndexBuffer = null!;
    // Holds the per-instance data for all instances
    internal InfiniteInstanceWriteOnlyBuffer<InstanceData> InfiniteInstanceBuffer = null!;

    public long Register(ICamera camera) => RegisteredCameras.Add(camera);
    public long Register(IMaskedRenderable maskedRenderable)
    {
        if (maskedRenderable is not IRenderable renderable)
            throw new ArgumentException("Unable to unmask an IRenderable");
        return RegisteredRenderables.Add(renderable);
    }
    public long Register(IMaskedLaminaRenderable maskedRenderable)
    {
        if (maskedRenderable is not ILaminaRenderable renderable)
            throw new ArgumentException("Unable to unmask an ILaminaRenderable");
        return RegisteredLaminaElements.Add(renderable);
    }
    
    public void UpdateRegistered(long rid, ICamera camera) => RegisteredCameras.Update(rid, camera);
    public void UpdateRegistered(long rid, IMaskedRenderable maskedRenderable)
    {
        if (maskedRenderable is not IRenderable renderable)
            throw new ArgumentException("Unable to unmask an IRenderable");
        RegisteredRenderables.Update(rid, renderable);
    }
    public void UpdateRegistered(long rid, IMaskedLaminaRenderable maskedRenderable)
    {
        if (maskedRenderable is not ILaminaRenderable renderable)
            throw new ArgumentException("Unable to unmask an ILaminaRenderable");
        RegisteredLaminaElements.Update(rid, renderable);
    }
    
    public void UnregisterCamera(long rid) => RegisteredCameras.Remove(rid);
    public void UnregisterRenderable(long rid) => RegisteredRenderables.Remove(rid);
    public void UnregisterLamina(long rid) => RegisteredLaminaElements.Remove(rid);

    public void InitializeResources(RenderingResources resources)
    {
        _renderDevice = resources.RenderDevice;
        _immediateContext = resources.ImmediateContext;
        _deferredContexts = resources.DeferredContexts;
        _swapChain = resources.SwapChain;
        
        ViewMatrixBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "ViewMatrixBuffer",
            Size = MatrixFloat.SizeInBytes,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        });
        
        _instanceIndexBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "InstanceIndexBuffer",
            Size = 16u,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
        });
        
        using var engineFactory = Native.GetEngineFactoryD3D12();
        InfiniteInstanceBuffer = new InfiniteInstanceWriteOnlyBuffer<InstanceData>();
        var renderContext = new RenderContext
        {
            ImmediateContext = _immediateContext,
            DeferredContexts = _deferredContexts,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
            ViewMatrixBuffer = ViewMatrixBuffer,
            ObjectIndexBuffer = _instanceIndexBuffer,
            InstanceBuffer = InfiniteInstanceBuffer,
            RenderTargetSize = new Vector2(_swapChain.GetDesc().Width, _swapChain.GetDesc().Height),
            ShaderFactory = engineFactory.CreateDefaultShaderSourceStreamFactory("Assets/Shaders"),
        };
        
        RenderContext.Current = renderContext;
    }
    
    public void InitializeRenderers()
    {
        _frameRenderLoop = new FrameRenderLoop(this, new TextRenderer(_immediateContext));
        
        CreateRenderTargets();
        
        // RootWindow.Render += RenderSingleFrameSync;
        RootWindow.FramebufferResize += OnFramebufferResize;
    }

    internal void CreateRenderTargets()
    {
        var swapChain = _swapChain.GetDesc();
        RenderTarget = _renderDevice.CreateTexture(new TextureDesc
        {
            Type = ResourceDimension.Tex2d,
            Width = (uint)FramebufferSize.X,
            Height = (uint)FramebufferSize.Y,
            MipLevels = 1,
            Format = swapChain.ColorBufferFormat,
            SampleCount = (uint)PipelineBuilder.MsaaSamples,
            BindFlags = BindFlags.RenderTarget
        });
        RenderTargetView = RenderTarget.GetDefaultView(TextureViewType.RenderTarget);
        
        _renderDepth = _renderDevice.CreateTexture(new TextureDesc
        {
            Type        = ResourceDimension.Tex2d,
            Width       = (uint)FramebufferSize.X,
            Height      = (uint)FramebufferSize.Y,
            MipLevels   = 1,
            Format      = swapChain.DepthBufferFormat,
            SampleCount = (uint)PipelineBuilder.MsaaSamples,
            BindFlags   = BindFlags.DepthStencil
        });
        RenderDepthView = _renderDepth.GetDefaultView(TextureViewType.DepthStencil);
    }
    
    [SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
    private void DisposeRenderTargets()
    {
        RenderTarget?.Dispose();
        _renderDepth?.Dispose();
    }

    public void RenderEngineLoadingScreen()
    {
        new LoadingSplashRenderLoop(this).RenderEngineLoadingScreen();
    }

    public void RenderSingleFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        RegisteredCameras.FlushPending();
        RegisteredRenderables.FlushPending();
        RegisteredLaminaElements.FlushPending();
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingFlushRegistrations);
        
        _frameRenderLoop.RenderSingleFrame(deltaTime);
    }
    
    private void OnFramebufferResize(Vector2D<int> size)
    {
        var width = size.X;
        var height = size.Y;

        if (width == 0 || height == 0)
            return;
        
        DisposeRenderTargets();
        _swapChain.Resize((uint)size.X, (uint)size.Y, SurfaceTransform.Identity);
        CreateRenderTargets();
        AssetManager.Shared.Pipelines.InvalidateAll();
    }

    public void ToggleLogRendering() => _frameRenderLoop.ToggleLogRendering();
    
    public void HotShutdown()
    { 
        // RootWindow.Render -= RenderSingleFrameSync;
        RootWindow.FramebufferResize -= OnFramebufferResize;
        try
        {
            _immediateContext.SetPipelineState(null);
            _immediateContext.Flush();
            _renderDevice.IdleGPU();
            AssetManager.Shared.Pipelines.InvalidateAll();
        }
        catch { /* ignored */ }

        ViewMatrixBuffer.Dispose();
        _instanceIndexBuffer.Dispose();
        InfiniteInstanceBuffer.Dispose();
        DisposeRenderTargets();
    }

    public void NotifyModuleReloaded(EngineModule module) {}
    public void NotifyGameplayContextChanged(GameplayContext context) {}
    
    // public long Register(LaminaRenderer renderer)
    // {
    //     // if (maskedParent is not Spatial parent)
    //     //     throw new ArgumentException("Unable to unmask a Spatial"); 
    //     // if (maskedComponent is not PhysicsComponent component)
    //     //     throw new ArgumentException("Unable to unmask a PhysicsComponent");
    //     return _registeredAtoms.Add(parent, component);
    // }
    // public void Unregister(long rid) => _registeredAtoms.Remove(rid);
}
