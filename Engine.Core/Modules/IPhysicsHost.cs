using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Modules;

public interface IPhysicsHost : IModularHost
{
    public void Register(long rid, ISpatial parent, IPhysicsComponent component);
    public void Unregister(long rid);
    public void RegisterService(ICacheRevalidationService service);
    public void UnregisterService(ICacheRevalidationService service);
    public void Initialize();
    public void Shutdown();
    public void ProcessPhysicsFrame(double deltaTime);
    public void RevalidateWorldTransform(ISpatial atom);
}