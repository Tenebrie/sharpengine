using System.Drawing;
using Diligent;
using Engine.Core.Common;
using Engine.Core.Modules.Assets;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Modules;

public interface IRenderingModuleBootstrap
{
    public IRootHypervisor Hypervisor { get; set; }
    public RenderingResources Initialize();
    public void Shutdown();
}

public struct RenderingResources
{
    public required IEngineFactoryD3D12 EngineFactory;
    public required IRenderDevice RenderDevice;
    public required IDeviceContext ImmediateContext;
    public required IDeviceContext[] DeferredContexts;
    public required ISwapChain SwapChain;
}

public interface ILaminaLayout;
public interface ILaminaWidgetRenderer;

public interface IRenderingHost : IModularHost
{
    public void UpdateRegistered(long rid, ICamera camera);
    public void UpdateRegistered(long rid, IMaskedRenderable maskedRenderable);
    public void UpdateRegistered(long rid, IMaskedLaminaRenderable maskedRenderable);
    
    public void UnregisterCamera(long rid);
    public void UnregisterRenderable(long rid);
    public void UnregisterLamina(long rid);
    
    public void InitializeResources(RenderingResources resources);
    public void InitializeRenderers();
    public void RenderEngineLoadingScreen();
    public void RenderSingleFrame(double deltaTime);
    public void ToggleLogRendering();
    public void HotShutdown();
}

public interface IMaskedRenderable;
public interface IMaskedLaminaRenderable;
