using Diligent;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Enum;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Modules;

public interface IRenderingModuleBootstrap
{
    public RenderingResources Initialize(IWindow window);
    public void Shutdown();
}

public struct RenderingResources
{
    public required IRenderDevice RenderDevice;
    public required IDeviceContext DeviceContext;
    public required ISwapChain SwapChain;
}

public interface IRenderingModule
{
    public void Register(Backstage backstage);
    public void Unregister(Backstage backstage);
    public void HotInitialize(RenderingResources resources, IWindow window);
    public void ToggleLogRendering();
    public void HotShutdown();
    public void SetGameplayContext(GameplayContext context);
}
