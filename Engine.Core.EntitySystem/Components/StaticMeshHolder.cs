using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Native.Bgfx;

namespace Engine.Core.EntitySystem.Components;

public partial class StaticMeshHolder : ActorComponent
{
    private StaticMesh? _mesh;
    public StaticMesh Mesh
    {
        get => _mesh ?? MeshAssetManager.FallbackMesh;
        set
        {
            _mesh?.OnMeshLoaded.Disconnect(OnMeshLoaded);
            _mesh = value;
            _mesh.OnMeshLoaded.Connect(this, OnMeshLoaded);
            if (_mesh.IsValid)
                OnMeshLoaded(_mesh.Vertices);
        }
    }

    private MaterialInstance? _material;
    public MaterialInstance Material
    {
        get => _material ?? MaterialAssetManager.FallbackMaterial;
        set => _material = value;
    }
    [Component] public BoundingSphereComponent BoundingSphere;

    private void OnMeshLoaded(AssetVertex[] vertices)
    {
        BoundingSphere.Generate(vertices);
    }

    public Bgfx.StateFlags RenderFlags { get; set; } = Bgfx.StateFlags.None;
}