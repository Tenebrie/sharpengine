using Engine.Assets;
using Engine.Assets.Loaders;
using Engine.Assets.Meshes;
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
        ObjMeshLoader.LoadObj("Assets/Meshes/decimated_dragon32.obj", out var vertices, out var indices);
        MeshComponent.Mesh = StaticMesh.CreateFromMemory(vertices, indices);
        MeshComponent.Material = AssetManager.LoadMaterial("Meshes/RawColor/RawColor");
        MeshComponent.BoundingSphere.Generate(vertices);
        
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(90, 0, 0);
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        // Logger.Info("test");
    }
}