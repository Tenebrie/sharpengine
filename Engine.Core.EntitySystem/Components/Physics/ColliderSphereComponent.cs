using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Physics;

[UsedImplicitly]
public partial class ColliderSphereComponent : ActorComponent
{
    public readonly SphereMesh Mesh = SphereMesh.Instance;

    public double Radius
    {
        get => Transform.Scale.X;
        set => Transform.Scale = new Vector3(value, value, value);
    }
    public double WorldRadius => WorldTransform.Scale.X;

    [OnInit]
    protected void OnInit()
    {
        Mesh.Load();
    }
}