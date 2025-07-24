using Engine.Codegen.Bgfx.Unsafe;
using Engine.Core.Enum;
using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.User.Contracts;

public interface IRendererContract
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