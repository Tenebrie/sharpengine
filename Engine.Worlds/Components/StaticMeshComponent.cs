using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Assets.Materials.Meshes.Wireframe;
using Engine.Assets.Meshes;
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Interfaces;
using JetBrains.Annotations;

namespace Engine.Worlds.Components;

[UsedImplicitly]
public class StaticMeshComponent : ActorComponent, IRenderable
{
    public StaticMesh Mesh;
    public Material Material;

    [OnInit]
    protected void OnInit()
    {
        Mesh = new StaticMesh();
        Material = new HonseTerrainMaterial();
    }
    
    private Transform[] _singleComponentTransforms = new Transform[1];
    public void Render()
    {
        _singleComponentTransforms[0] = WorldTransform;
        Mesh.Render(1, ref _singleComponentTransforms, 0, Material);
        Mesh.BoundingSphere.Render(1, ref _singleComponentTransforms, 0, WireframeMaterial.Instance);
    }
}
