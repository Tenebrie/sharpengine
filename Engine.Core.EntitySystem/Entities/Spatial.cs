using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Primitives;
using Engine.Core.Modules.EntitySystem;
using Microsoft.Extensions.ObjectPool;

namespace Engine.Core.EntitySystem.Entities;

public abstract partial class Spatial : Atom, ISpatial
{
    private static readonly ObjectPool<Transform> SharedTransformPool = new DefaultObjectPool<Transform>(new DefaultPooledObjectPolicy<Transform>(), 1000);

    private Transform _transform = Transform.Identity;
    public Transform Transform
    {
        get => _transform;
        set => _transform = ClaimedTransform.Claim(value, this);
    }
    
    public ref Transform TransformReference => ref _transform;

    protected Spatial()
    {
        Transform = SharedTransformPool.Get();
    }

    private bool _ignoreParentPosition = false;

    private bool _cachedWorldTransformValid = false;
    private Transform _cachedWorldTransform = SharedTransformPool.Get();
    private bool _cachedWorldTransformInverseValid = false;
    private Transform _cachedWorldTransformInverse = SharedTransformPool.Get();
    private bool _cachedWorldTransformNoScaleValid = false;
    private Transform _cachedWorldTransformNoScale = SharedTransformPool.Get();
    private bool _cachedWorldTransformOwnScaleOnlyValid = false;
    private Transform _cachedWorldTransformOwnScaleOnly = SharedTransformPool.Get();
    public Transform WorldTransform
    {
        get
        {
            if (_cachedWorldTransformValid)
                return _cachedWorldTransform;

            if (Parent is not Spatial parent || _ignoreParentPosition)
            {
                Transform.Copy(Transform, ref _cachedWorldTransform);
                _cachedWorldTransformValid = true;
                return _cachedWorldTransform;
            }
            
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
    public Transform WorldTransformNoScale
    {
        get
        {
            if (_cachedWorldTransformNoScaleValid)
                return _cachedWorldTransformNoScale;

            if (Parent is not Spatial parent || _ignoreParentPosition)
            {
                Transform.Copy(Transform, ref _cachedWorldTransformNoScale);
                _cachedWorldTransformNoScale.Scale = Vector3.One;
                _cachedWorldTransformNoScaleValid = true;
                return _cachedWorldTransformNoScale;
            }

            parent.WorldTransformNoScale.Multiply(Transform, ref _cachedWorldTransformNoScale);
            _cachedWorldTransformNoScale.Scale = Vector3.One;
            _cachedWorldTransformNoScaleValid = true;
            return _cachedWorldTransformNoScale;
        }
    }

    public Transform WorldTransformOwnScaleOnly
    {
        get
        {
            if (_cachedWorldTransformOwnScaleOnlyValid)
                return _cachedWorldTransformOwnScaleOnly;

            if (Parent is not Spatial parent || _ignoreParentPosition)
            {
                Transform.Copy(Transform, ref _cachedWorldTransformOwnScaleOnly);
                _cachedWorldTransformOwnScaleOnlyValid = true;
                return _cachedWorldTransformOwnScaleOnly;
            }

            parent.WorldTransformNoScale.Multiply(Transform, ref _cachedWorldTransformOwnScaleOnly);
            _cachedWorldTransformOwnScaleOnlyValid = true;
            return _cachedWorldTransformOwnScaleOnly;
        }
    }
     
    internal void InvalidateWorldTransform() 
    {
        _cachedWorldTransformValid = false;
        _cachedWorldTransformInverseValid = false;
        _cachedWorldTransformNoScaleValid = false;
        _cachedWorldTransformOwnScaleOnlyValid = false;
    }
 
    [OnDestroy] 
    protected void OnSpatialDestroy()
    {
        _transform.ResetToIdentity();
        _cachedWorldTransform.ResetToIdentity();
        _cachedWorldTransformInverse.ResetToIdentity();
        _cachedWorldTransformNoScale.ResetToIdentity();
        _cachedWorldTransformOwnScaleOnly.ResetToIdentity();
        
        SharedTransformPool.Return(_transform); 
        SharedTransformPool.Return(_cachedWorldTransform);
        SharedTransformPool.Return(_cachedWorldTransformInverse);
        SharedTransformPool.Return(_cachedWorldTransformNoScale);
        SharedTransformPool.Return(_cachedWorldTransformOwnScaleOnly);
        _transform = null!;
        _cachedWorldTransform = null!;
        _cachedWorldTransformInverse = null!;
        _cachedWorldTransformNoScale = null!;
        _cachedWorldTransformOwnScaleOnly = null!;
    }
    
    public void IgnoreParentPosition() => _ignoreParentPosition = true;
}        
 
public static class SpatialExternals
{
    public static void InvalidateWorldTransform(Spatial spatial) => spatial.InvalidateWorldTransform();
}
