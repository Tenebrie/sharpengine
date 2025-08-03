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

public static class SpatialExternals
{
    public static void InvalidateWorldTransform(Spatial spatial) => spatial.InvalidateWorldTransform();
}
