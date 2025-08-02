using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;

namespace Engine.Core.EntitySystem.Primitives;

public class ClaimedTransform : Transform
{
    private readonly Spatial? _owner;
    
    private ClaimedTransform(Spatial owner, Transform baseTransform)
    {
        _owner = owner;
        Data = baseTransform.ToMatrix();
    }

    public override Vector3 Position
    {
        get => base.Position;
        set
        {
            base.Position = value;
            InvalidateCache();
        }
    }
    
    public override Quat Rotation
    {
        get => base.Rotation;
        set
        {
            base.Rotation = value;
            InvalidateCache();
        }
    }
    
    public override Vector3 Scale
    {
        get => base.Scale;
        set
        {
            base.Scale = value;
            InvalidateCache();
        }
    }

    public static Transform Claim(Transform baseTransform, Spatial forOwner) => new ClaimedTransform(forOwner, baseTransform);

    private void InvalidateCache()
    {
        if (_owner == null || !Atom.IsValid(_owner))
            return;
        _owner?.InvalidateWorldTransform();
        _owner?.GetService<CacheRevalidationService>().InvalidateTransform(_owner);
    }
}
