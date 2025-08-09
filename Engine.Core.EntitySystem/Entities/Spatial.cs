using Engine.Core.Common;
using Engine.Core.EntitySystem.Primitives;

namespace Engine.Core.EntitySystem.Entities;

public abstract partial class Spatial : Atom
{
    private Transform _transform = null!;
    public Transform Transform
    {
        get => _transform;
        set => _transform = ClaimedTransform.Claim(value, this);
    }
    
    public ref Transform TransformReference => ref _transform;

    protected Spatial()
    {
        Transform = Transform.Identity;
    }

    private bool _cachedWorldTransformValid = false;
    private Transform _cachedWorldTransform = Transform.Identity;
    private bool _cachedWorldTransformInverseValid = false;
    private Transform _cachedWorldTransformInverse = Transform.Identity;
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
    public Transform WorldTransformInverse
    {
        get
        {
            if (_cachedWorldTransformInverseValid)
                return _cachedWorldTransform;

            WorldTransform.GetInverse(ref _cachedWorldTransformInverse);
            _cachedWorldTransformInverseValid = true;
            return _cachedWorldTransformInverse;
        }
    }
    
    internal void InvalidateWorldTransform()
    {
        _cachedWorldTransformValid = false;
        _cachedWorldTransformInverseValid = false;
    }
}

public static class SpatialExternals
{
    public static void InvalidateWorldTransform(Spatial spatial) => spatial.InvalidateWorldTransform();
}
