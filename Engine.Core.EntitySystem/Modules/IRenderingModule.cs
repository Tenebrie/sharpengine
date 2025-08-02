using Engine.Native.Bgfx;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Enum;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Modules;

public interface IRenderingModule
{
    public void Register(Backstage backstage);
    public void Unregister(Backstage backstage);
    public void Initialize(IWindow window);
    public void HotInitialize(IWindow window);
    public void ToggleResetFlags(Bgfx.ResetFlags flags);
    public void ToggleDebugFlags(Bgfx.DebugFlags flags);
    public void ToggleLogRendering();
    public void DisconnectCallbacks();
    public void Shutdown();
    public void SetGameplayContext(GameplayContext context);
}
