using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Core.Common;
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
        Material = new UnlitMaterial();
        Mesh = new StaticMesh();
        Mesh.LoadUnitCube();
    }
}