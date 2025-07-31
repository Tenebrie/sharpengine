using Engine.Assets;
using Engine.Assets.Loaders;
using Engine.Assets.Meshes;
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
        MeshComponent.Mesh = AssetManager.LoadMesh("Assets/Meshes/terrain-plain.obj");
        MeshComponent.Material = AssetManager.LoadMaterial("Meshes/HonseTerrain/HonseTerrain");
        // MeshComponent.Material = AssetManager.LoadMaterial("Meshes/HonseTerrain/HonseTerrain");
        MeshComponent.Material.LoadTexture("Assets/Textures/honse-terrain.png");
    }
}
