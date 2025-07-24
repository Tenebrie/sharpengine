using Engine.Core.Common;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;

namespace Engine.Worlds.Primitives;

public class ClaimedTransform : Transform
{
    private readonly Spatial? _owner;
    
    private ClaimedTransform(Spatial owner, Transform baseTransform)
    {
        _owner = owner;
        Data = baseTransform.ToMatrix();
    }

    public override Vector Position
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
    
    public override Vector Scale
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
