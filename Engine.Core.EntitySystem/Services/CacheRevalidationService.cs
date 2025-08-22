using Engine.Core.DataStructures;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.EntitySystem.Services;

public partial class CacheRevalidationService : Service, ICacheRevalidationService
{
    private readonly ThreadLocalHashSet<Spatial> _transformInvalidatedAtoms = new();
    private HashSet<Spatial> _collectedAtoms = [];
    
    public bool Disabled { get; set; } = false;
    
    internal void InvalidateTransform(Spatial atom)
    {
        if (Disabled || atom.GetChild<PhysicsComponent>() != null)
            return;
        _transformInvalidatedAtoms.Add(atom);
    }

    [OnReady]
    protected void OnReady()
    {
        Backstage.PhysicsModule?.RegisterService(this);
    }
    
    [OnUpdate]
    internal void OnUpdate()
    {
        _collectedAtoms.Clear();
        _transformInvalidatedAtoms.Collect(ref _collectedAtoms);
        foreach (var atom in _collectedAtoms)
        {
            if (!IsValid(atom))
                continue;
        
            Backstage.PhysicsModule?.RevalidateWorldTransform(atom);
        }
    }
    
    [OnDestroy]
    protected void OnDestroy()
    {
        Backstage.PhysicsModule?.UnregisterService(this);
        _transformInvalidatedAtoms.Dispose();
    }
}
