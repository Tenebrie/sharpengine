using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Modules;

public interface IPhysicsHost : IModularHost
{
    public long Register(ISpatial parent, IPhysicsComponent component);
    public void Unregister(long rid);
    public void RegisterService(ICacheRevalidationService service);
    public void UnregisterService(ICacheRevalidationService service);
    public void Initialize();
    public void Shutdown();
    public void ProcessPhysicsFrame(double deltaTime);
    public void RevalidateWorldTransform(ISpatial atom);
}