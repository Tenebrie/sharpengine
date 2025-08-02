using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components;

[UsedImplicitly]
public partial class BoundingSphereComponent : ActorComponent
{
    public readonly SphereMesh Mesh = SphereMesh.Instance;
    
    public double Radius => Transform.Scale.X;
    public double WorldRadius => WorldTransform.Scale.X;

    [OnInit]
    protected void OnInit()
    {
        Mesh.Load();
    }
    
    public void Generate(AssetVertex[] verts)
    {
        if (verts.Length < 3)
        {
            return;
        }

        try
        {
            CalculateRittersBoundingSphere(verts);
        } catch (Exception ex)
        {
            Logger.Error("Failed to calculate bounding sphere: " + ex.Message);
        }
    }

    private void CalculateRittersBoundingSphere(AssetVertex[] verts)
    {
        var p0 = verts[0].Position;
        var p1 = verts.OrderByDescending(v => v.Position.DistanceSquaredTo(p0)).First().Position;
        var p2 = verts.OrderByDescending(v => v.Position.DistanceSquaredTo(p1)).First().Position;
        var center = (p1 + p2) * 0.5;
        var radius = p1.DistanceTo(p2) * 0.5;

        foreach (var v in verts)
        {
            var d = v.Position.DistanceTo(center);
            if (!(d > radius))
                continue;
            
            var newRadius = (radius + d) * 0.5;
            var direction = (v.Position - center) / d;
            center += direction * (newRadius - radius);
            radius = newRadius;
        }
        
        Transform.Position = center;
        Transform.Rotation = Quat.Identity;
        Transform.Scale = new Vector3(radius, radius, radius);
    }
}