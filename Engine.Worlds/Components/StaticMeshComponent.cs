using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Assets.Meshes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Components;

public class StaticMeshComponent : ActorComponent
{
    public StaticMesh Mesh;
    public Material Material;

    [OnInit]
    protected void OnInit()
    {
        Mesh = new StaticMesh();
        Material = new HonseTerrainMaterial();
    }
}
