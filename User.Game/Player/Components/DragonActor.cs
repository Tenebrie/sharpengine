using Engine.Assets.Loaders;
using Engine.Assets.Materials.Meshes.RawColor;
using Engine.Core.Makers;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Player.Components;

public class DragonMesh : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    
    [OnInit]
    protected void OnInit()
    {
        // ObjMeshLoader.LoadObj("Assets/Meshes/projectile-sword.obj", out var vertices, out var indices);
        ObjMeshLoader.LoadObj("Assets/Meshes/decimated_dragon32.obj", out var vertices, out var indices);
        MeshComponent.Mesh.Load(vertices, indices);
        MeshComponent.Material = new RawColorMaterial();
        
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(90, 0, 0);
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        // Logger.Info("test");
    }
}