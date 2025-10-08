using System.Diagnostics.CodeAnalysis;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Multithreading;
using Engine.Core.Enum;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Module.Rendering.Renderers;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Renderers.Splash;
using Engine.Module.Rendering.Utilities;
using JetBrains.Annotations;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using MapFlags = Diligent.MapFlags;
using ResourceDimension = Diligent.ResourceDimension;
using Usage = Diligent.Usage;

namespace Engine.Module.Rendering;

[UsedImplicitly]
public class RenderingHost : IRenderingHost
{
    internal List<Backstage> Backstages = [];

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

    private Vector2D<int> FramebufferSize => RootWindow.GetScaledFramebufferSize();

    // Constant camera matrix buffer
    internal IBuffer ViewMatrixBuffer = null!;
    
    // Contains the index of the current instance
    private IBuffer _instanceIndexBuffer = null!;
    // Holds the per-instance data for all instances
    internal InfiniteInstanceWriteOnlyBuffer<InstanceData> InfiniteInstanceBuffer = null!;
    
    public void InitializeResources(RenderingResources resources)
    {
        _renderDevice = resources.RenderDevice;
        _immediateContext = resources.ImmediateContext;
        _deferredContexts = resources.DeferredContexts;
        _swapChain = resources.SwapChain;
        
        ViewMatrixBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
            Size = MatrixFloat.SizeInBytes,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.UniformBuffer,
            CPUAccessFlags = CpuAccessFlags.Write
        });
        
        _instanceIndexBuffer = resources.RenderDevice.CreateBuffer(new BufferDesc
        {
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
        
        RootWindow.Render += RenderSingleFrameSync;
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

    private void RenderSingleFrameSync(double deltaTime)
    {
        _frameRenderLoop.RenderSingleFrame(deltaTime).GetAwaiter().GetResult();
    }
    
    public void RenderEngineLoadingScreen()
    {
        new LoadingSplashRenderLoop(this).RenderEngineLoadingScreen();
    }

    public Task RenderSingleFrame(double deltaTime)
    {
        return _frameRenderLoop.RenderSingleFrame(deltaTime);
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
        RootWindow.Render -= RenderSingleFrameSync;
        RootWindow.FramebufferResize -= OnFramebufferResize;
        try
        {
            _immediateContext.SetPipelineState(null);
            _immediateContext.Flush();
            _renderDevice.IdleGPU();
            AssetManager.Shared.Pipelines.InvalidateAll();
        }
        catch { /* ignored */ }

        Backstages = [];
        ViewMatrixBuffer.Dispose();
        _instanceIndexBuffer.Dispose();
        InfiniteInstanceBuffer.Dispose();
        DisposeRenderTargets();
    }

    public void NotifyModuleReloaded(EngineModule module)
    {
        Backstages.Clear();
        if (Hypervisor.GameplayModule is Backstage gameplayHost)             
            Backstages.Add(gameplayHost);
        if (Hypervisor.WorkspaceModule is Backstage workspaceHost)
            Backstages.Add(workspaceHost);
    }
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
