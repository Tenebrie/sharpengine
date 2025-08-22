using Diligent;
using Engine.Core.Enum;
using Engine.Core.Modules.EntitySystem;
using Silk.NET.Windowing;

namespace Engine.Core.Modules;

public interface IRenderingModuleBootstrap
{
    public IRootHypervisor Hypervisor { get; set; }
    public RenderingResources Initialize();
    public void Shutdown();
}

public struct RenderingResources
{
    public required IRenderDevice RenderDevice;
    public required IDeviceContext ImmediateContext;
    public required IDeviceContext[] DeferredContexts;
    public required ISwapChain SwapChain;
}

public interface IRenderingHost : IModularHost
{
    public void HotInitialize(RenderingResources resources);
    public void RenderEngineLoadingScreen();
    public void ToggleLogRendering();
    public void HotShutdown();
}
