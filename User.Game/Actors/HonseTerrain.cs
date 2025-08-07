using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
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
        MeshComponent.Mesh = AssetManager.Meshes.LoadFromDisk("Meshes/terrain-plain.obj");
        MeshComponent.Material = AssetManager.Materials
            .Instantiate("Meshes/HonseTerrain/HonseTerrain")
            .SetTexture(Texture.CreateFromDisk("Textures/honse-terrain.png"));
    }
}
