using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Physics;

[UsedImplicitly]
public partial class ColliderSphereComponent : ActorComponent, IRenderable
{
    public double Radius
    {
        get => Transform.Scale.X;
        set => Transform.Scale = new Vector3(value, value, value);
    }
    public double WorldRadius => WorldTransform.Scale.X;

    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(WorldTransform, WorldRadius, null);
    public int GetInstanceCount() => 1;
    
    private readonly Transform[] _singleComponentTransforms = new Transform[1];
    public void Render()
    {
        _singleComponentTransforms[0] = WorldTransform;
        // LineSphereMesh.Shared.Render(1, _singleComponentTransforms, [WireframeMaterial.Shared], LineSphereMesh.ColorMode.Collider);
    }
}