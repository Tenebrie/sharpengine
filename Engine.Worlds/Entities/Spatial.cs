using Engine.Core.Common;
using Engine.Worlds.Primitives;

namespace Engine.Worlds.Entities;

public abstract partial class Spatial : Atom
{
    private Transform _transform = null!;
    public Transform Transform
    {
        get => _transform;
        set => _transform = ClaimedTransform.Claim(value, this);
    }

    protected Spatial()
    {
        Transform = Transform.Identity;
    }

    private bool _cachedWorldTransformValid = false;
    private Transform _cachedWorldTransform = Transform.Identity;
    public Transform WorldTransform
    {
        get
        {
            if (_cachedWorldTransformValid)
                return _cachedWorldTransform;

            if (Parent is not Spatial parent)
                return Transform;
            
            parent.WorldTransform.Multiply(Transform, ref _cachedWorldTransform);
            _cachedWorldTransformValid = true;
            return _cachedWorldTransform;
        }
    }
    
    internal void InvalidateWorldTransform()
    {
        _cachedWorldTransformValid = false;
    }
}
