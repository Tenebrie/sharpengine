using Engine.Assets.Materials;
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
        // var stopwatch = new System.Diagnostics.Stopwatch();
        Mesh = new StaticMesh();
        Material = new UnlitMaterial();
        // stopwatch.Start();
        Mesh.LoadUnitCube();
        // stopwatch.Stop();
    }
}
