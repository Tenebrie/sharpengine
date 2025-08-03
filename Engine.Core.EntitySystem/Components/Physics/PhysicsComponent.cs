using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Components.Physics;

public partial class PhysicsComponent : ActorComponent
{
    public long Rid = -1;
    public Vector3 Velocity = Vector3.Zero;

    private Spatial GetSpatialParent()
    {
        var parent = GetParent<Spatial>();
        if (parent == null)
            throw new InvalidOperationException("PhysicsComponent must be attached to a Spatial atom.");
        return parent;
    }

    [OnInit]
    protected void OnRegisterOnPhysicsServer()
    {
        var parent = GetSpatialParent();
        var physicsModule = Backstage.PhysicsModule;
        if (physicsModule == null)
        {
            Logger.Warn("PhysicsComponent could not register on PhysicsServer: Reference is null.");
            return;
        }
        Rid = physicsModule.Register(parent, this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnPhysicsServer()
    {
        if (Rid == -1)
            return;
        var physicsModule = Backstage.PhysicsModule;
        if (physicsModule == null)
        {
            Logger.Warn("PhysicsComponent could not register on PhysicsServer: Reference is null.");
            return;
        }
        physicsModule.Unregister(Rid);
    }

    public List<ColliderSphereComponent> GetSphereColliders()
    {
        return GetSpatialParent().GetChildren<ColliderSphereComponent>();
    }
}
