using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.User.Contracts;

public interface IRendererContract
{
    public void Register(Backstage backstage);
    public void Unregister(Backstage backstage);
    public void Initialize(IWindow window, WindowOptions opts);
    public void HotInitialize(IWindow window, WindowOptions opts);
    public void DisconnectCallbacks();
    public void Shutdown();
}