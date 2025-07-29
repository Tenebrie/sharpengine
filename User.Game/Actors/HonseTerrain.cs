using Engine.Assets.Loaders;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class HonseTerrain : Actor
{
    [Component]
    public StaticMeshComponent MeshComponent;
    
    [OnInit]
    protected void OnInit()
    {
        MeshComponent.Material = new HonseTerrainMaterial();
        ObjMeshLoader.LoadObj("Assets/Meshes/terrain-plain.obj", out var vertices, out var indices);
        MeshComponent.Mesh.Load(vertices, indices);
        MeshComponent.BoundingSphere.Generate(vertices);
        MeshComponent.Material.LoadTexture("Assets/Textures/honse-terrain.png");
    }
}
