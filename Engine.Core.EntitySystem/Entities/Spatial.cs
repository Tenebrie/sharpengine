using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Primitives;
using Engine.Core.Modules.EntitySystem;
using Microsoft.Extensions.ObjectPool;

namespace Engine.Core.EntitySystem.Entities;

public abstract partial class Spatial : Atom, ISpatial
{
    private static readonly ObjectPool<Transform> SharedTransformPool = new DefaultObjectPool<Transform>(new DefaultPooledObjectPolicy<Transform>(), 1000);
    
    private Transform _transform = null!;
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

    private bool _cachedWorldTransformValid = false;   
    private Transform _cachedWorldTransform = SharedTransformPool.Get();
    private bool _cachedWorldTransformInverseValid = false;
    private Transform _cachedWorldTransformInverse = SharedTransformPool.Get(); 
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
 
    [OnDestroy]
    protected void OnSpatialDestroy() 
    {
        _transform.ResetToIdentity();
        _cachedWorldTransform.ResetToIdentity();        
        _cachedWorldTransformInverse.ResetToIdentity();
        
        SharedTransformPool.Return(_transform); 
        SharedTransformPool.Return(_cachedWorldTransform);
        SharedTransformPool.Return(_cachedWorldTransformInverse);
        _transform = null!;
        _cachedWorldTransform = null!;   
        _cachedWorldTransformInverse = null!;   
    }   
}        
 
public static class SpatialExternals
{
    public static void InvalidateWorldTransform(Spatial spatial) => spatial.InvalidateWorldTransform();
}
