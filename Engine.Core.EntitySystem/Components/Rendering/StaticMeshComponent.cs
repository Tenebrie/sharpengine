using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Materials.Meshes.Wireframe;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Native.Bgfx;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class StaticMeshComponent : ActorComponent, IRenderable
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
    public Bgfx.StateFlags RenderFlags
    {
        get => _staticMeshHolder.RenderFlags;
        set => _staticMeshHolder.RenderFlags = value;
    }
    
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(BoundingSphere, null);
    public int GetInstanceCount() => 2;
    
    private Transform[] _singleComponentTransforms = new Transform[1];
    
    public void PrepareRender(ref RenderContext renderContext)
    {
        _singleComponentTransforms[0] = WorldTransform;
        Mesh.PrepareRender(1, ref _singleComponentTransforms, [Material], ref renderContext);
        _singleComponentTransforms[0] = BoundingSphere.WorldTransform;
        LineSphereMesh.Shared.PrepareRender(1, ref _singleComponentTransforms, [WireframeMaterial.Shared], ref renderContext);
    }
    public void Render(ref RenderContext renderContext)
    {
        Mesh.Render(1, Material, ref renderContext, RenderFlags);
        LineSphereMesh.Shared.Render(1, WireframeMaterial.Shared, ref renderContext, LineSphereMesh.ColorMode.AxisColor);
    }
}
