using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.Terrain;
using Engine.Assets.Materials.Meshes.Wireframe;
using Engine.Assets.Meshes;
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Interfaces;
using JetBrains.Annotations;

namespace Engine.Worlds.Components;

[UsedImplicitly]
public class TerrainMeshComponent : ActorComponent, IRenderable
{
    public StaticMesh Mesh;
    public Material Material;
    [Component] public BoundingSphereComponent BoundingSphere;
    
    [OnInit]
    protected void OnInit()
    {
        Mesh = new StaticMesh();
        Material = new TerrainMaterial();
        ObjMeshLoader.LoadObj("Assets/Meshes/terrain-plain.obj", out var vertices, out var indices);
        Mesh.Load(vertices, indices);
        BoundingSphere.Generate(vertices);
    }
    
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(BoundingSphere, null);
    
    private Transform[] _singleComponentTransforms = new Transform[1];
    public void Render()
    {
        _singleComponentTransforms[0] = WorldTransform;
        Mesh.Render(1, ref _singleComponentTransforms, 0, Material);
        _singleComponentTransforms[0] = BoundingSphere.WorldTransform;
        BoundingSphere.Mesh.Render(1, ref _singleComponentTransforms, 0, WireframeMaterial.Instance);
    }
}
