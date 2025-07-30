using Engine.Assets;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.Terrain;
using Engine.Assets.Materials.Meshes.Wireframe;
using Engine.Assets.Meshes;
using Engine.Assets.Meshes.Builtins;
using Engine.Assets.Rendering;
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
    public MaterialInstance Material;
    [Component] public BoundingSphereComponent BoundingSphere;
    
    [OnInit]
    protected void OnInit()
    {
        ObjMeshLoader.LoadObj("Assets/Meshes/terrain-plain.obj", out var vertices, out var indices);
        Mesh = StaticMesh.CreateFromMemory(vertices, indices);
        Material = AssetManager.LoadMaterial("Meshes/Terrain/Terrain");
        BoundingSphere.Generate(vertices);
    }
    
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(BoundingSphere, null);
    public int GetInstanceCount() => 2;

    private Transform[] _singleComponentTransforms = new Transform[1];
    public void PrepareRender(ref RenderContext renderContext)
    {
        _singleComponentTransforms[0] = WorldTransform;
        Mesh.PrepareRender(1, ref _singleComponentTransforms, ref renderContext);
        _singleComponentTransforms[0] = BoundingSphere.WorldTransform;
        BoundingSphereMesh.PrepareRender(1, ref _singleComponentTransforms, ref renderContext);
    }
    public void Render(ref RenderContext renderContext)
    {
        _singleComponentTransforms[0] = WorldTransform;
        Mesh.Render(1, Material, ref renderContext);
        _singleComponentTransforms[0] = BoundingSphere.WorldTransform;
        BoundingSphere.Mesh.Render(1, WireframeMaterial.Instance, ref renderContext);
    }
}
