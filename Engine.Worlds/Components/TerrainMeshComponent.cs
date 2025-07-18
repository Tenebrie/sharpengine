using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Components;

public class TerrainMeshComponent : ActorComponent
{
    public StaticMesh Mesh;
    public Material Material;
    
    [OnInit]
    protected void OnInit()
    {
        Mesh = new StaticMesh();
        Material = new TerrainMaterial();
        ObjMeshLoader.LoadObj("Assets/Meshes/terrain-plain.obj", out var vertices, out var indices);
        Mesh.Load(vertices, indices);
    }
}
