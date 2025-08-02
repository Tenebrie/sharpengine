using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Services;

public partial class CacheRevalidationService : Service
{
    private readonly HashSet<Spatial> _transformInvalidatedAtoms = [];
    
    internal void InvalidateTransform(Spatial atom)
    {
        _transformInvalidatedAtoms.Add(atom);
    }
    
    [OnUpdate]
    internal void OnUpdate()
    {
        foreach (var atom in _transformInvalidatedAtoms)
        {
            if (!IsValid(atom))
                continue;

            RevalidateSpatial(atom);
        }
        _transformInvalidatedAtoms.Clear();
    }

    private static void RevalidateSpatial(Spatial atom)
    {
        atom.InvalidateWorldTransform();
        _ = atom.WorldTransform;
        foreach (var child in atom.Children)
        {
            if (child is not Spatial spatial)
                continue;
            RevalidateSpatial(spatial);
        }
    }
}
