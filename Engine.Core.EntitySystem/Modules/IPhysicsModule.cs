using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;

namespace Engine.Core.EntitySystem.Modules;

public interface IPhysicsModule
{
    public long Register(Spatial parent, PhysicsComponent component);
    public void Unregister(long rid);
    public void RegisterService(CacheRevalidationService service);
    public void UnregisterService(CacheRevalidationService service);
    public void Initialize();
    public void ProcessPhysicsFrame(double deltaTime);
    public void RevalidateWorldTransform(Spatial atom);
}