using Engine.Core.Assets;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class HonseTerrain : Actor
{
    [Component]
    public StaticMeshComponent MeshComponent;
    
    [OnReady]
    protected void OnReady()
    {
        MeshComponent.Mesh = AssetManager.LoadMesh("Assets/Meshes/terrain-plain.obj");
        MeshComponent.Material = AssetManager.InstantiateMaterial("Meshes/HonseTerrain/HonseTerrain");
        // MeshComponent.Material = AssetManager.LoadMaterial("Meshes/HonseTerrain/HonseTerrain");
        MeshComponent.Material.LoadTexture("Assets/Textures/honse-terrain.png");
    }
}
