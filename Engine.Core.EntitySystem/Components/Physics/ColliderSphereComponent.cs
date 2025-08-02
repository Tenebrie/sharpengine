using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Physics;

[UsedImplicitly]
public partial class ColliderSphereComponent : ActorComponent
{
    public readonly SphereMesh Mesh = SphereMesh.Instance;
    
    public double Radius => Transform.Scale.X;
    public double WorldRadius => WorldTransform.Scale.X;

    [OnInit]
    protected void OnInit()
    {
        Mesh.Load();
    }
}