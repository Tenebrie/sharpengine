using Engine.Core.Assets.Materials.Meshes.Wireframe;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
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
    
    private Transform[] _singleComponentTransforms = new Transform[1];

    public void PrepareRender(ref RenderContext renderContext)
    {
        _singleComponentTransforms[0] = WorldTransform;
        SphereMesh.PrepareRender(1, ref _singleComponentTransforms, ref renderContext);
    }
    public void Render(ref RenderContext renderContext)
    {
        SphereMesh.Render(1, WireframeMaterial.Instance, ref renderContext, SphereMesh.ColorMode.Collider);
    }
}