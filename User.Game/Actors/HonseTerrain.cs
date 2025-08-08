using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
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
        MeshComponent.Mesh = StaticMesh.CreateFromDisk("Meshes/terrain-plain.obj");
        MeshComponent.Material = Material.CreateFromDisk("Meshes/HonseTerrain/HonseTerrain")
            .Instantiate()
            .LoadTexture(Texture.CreateFromDisk("Textures/honse-terrain.png"));
    }
}
