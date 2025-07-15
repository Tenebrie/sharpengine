using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Primitives;

namespace Engine.Worlds.Entities;

public abstract class Spatial : Atom
{
    private Transform _transform;
    public Transform Transform
    {
        get => _transform;
        set => _transform = ClaimedTransform.Claim(value, this);
    }

    protected Spatial()
    {
        Transform = Transform.Identity;
    }

    private Transform? _cachedWorldTransform;
    public Transform WorldTransform
    {
        get
        {
            if (_cachedWorldTransform is not null)
                return _cachedWorldTransform;

            if (Parent is not Spatial parent)
                return Transform;
            
            _cachedWorldTransform = Transform * parent.WorldTransform;
            return _cachedWorldTransform;
        }
    }
    
    internal void InvalidateWorldTransform()
    {
        _cachedWorldTransform = null;
    }
}
