using Engine.Core.DataStructures;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Services;

public partial class CacheRevalidationService : Service
{
    private readonly ThreadLocalHashSet<Spatial> _transformInvalidatedAtoms = new();
    private HashSet<Spatial> _collectedAtoms = [];
    
    public bool Disabled { get; set; } = false;
    
    internal void InvalidateTransform(Spatial atom)
    {
        if (Disabled)
            return;
        _transformInvalidatedAtoms.Add(atom);
    }

    [OnInit]
    protected void OnInit()
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
    }
}
