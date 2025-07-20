using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Services;

public class CacheRevalidationService : Service
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
