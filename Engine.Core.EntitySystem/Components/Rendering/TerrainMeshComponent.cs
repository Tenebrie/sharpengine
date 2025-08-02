using Engine.Core.Assets;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Materials.Meshes.Wireframe;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class TerrainMeshComponent : ActorComponent, IRenderable
{
    [Component] private StaticMeshHolder _staticMeshHolder;
    public StaticMesh Mesh
    {
        get => _staticMeshHolder.Mesh;
        set => _staticMeshHolder.Mesh = value;
    }
    public MaterialInstance Material
    {
        get => _staticMeshHolder.Material;
        set => _staticMeshHolder.Material = value;
    }
    public BoundingSphereComponent BoundingSphere
    {
        get => _staticMeshHolder.BoundingSphere;
        set => _staticMeshHolder.BoundingSphere = value;
    }
    
    [OnInit]
    protected void OnInit()
    {
        ObjMeshLoader.LoadObj("Assets/Meshes/terrain-plain.obj", out var vertices, out var indices);
        Mesh = StaticMesh.CreateFromMemory(vertices, indices);
        Material = AssetManager.LoadMaterial("Meshes/Terrain/Terrain");
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
        SphereMesh.PrepareRender(1, ref _singleComponentTransforms, ref renderContext);
    }
    public void Render(ref RenderContext renderContext)
    {
        Mesh.Render(1, Material, ref renderContext);
        BoundingSphere.Mesh.Render(1, WireframeMaterial.Instance, ref renderContext);
    }
}
