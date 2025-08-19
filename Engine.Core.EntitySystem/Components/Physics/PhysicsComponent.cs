using System.Buffers;
using Engine.Core.Attributes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Modules;

namespace Engine.Core.EntitySystem.Components.Physics;

public partial class PhysicsComponent : ActorComponent
{
    public long Rid = -1;
    public bool GravityEnabled = false;
    public Vector3 LinearVelocity = Vector3.Zero;
    public Vector3 AngularVelocity = Vector3.Zero;

    private Spatial GetSpatialParent()
    {
        var parent = GetParent<Spatial>();
        if (parent == null)
            throw new InvalidOperationException("PhysicsComponent must be attached to a Spatial atom.");
        return parent;
    }

    [OnCreate]
    [OnModuleReload(EngineModule.Physics)]
    protected void OnRegisterOnPhysicsServer()
    {
        var parent = GetSpatialParent();
        var physicsModule = Backstage.PhysicsModule;
        if (physicsModule == null)
            return;
        Rid = physicsModule.Register(parent, this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnPhysicsServer()
    {
        if (Rid == -1)
            return;
        var physicsModule = Backstage.PhysicsModule;
        physicsModule?.Unregister(Rid);
    }

    private readonly List<ColliderSphereComponent> _sphereColliders = []; 
    public List<ColliderSphereComponent> GetSphereColliders()
    {
        return GetSpatialParent().GetChildren(_sphereColliders);
    }
}
