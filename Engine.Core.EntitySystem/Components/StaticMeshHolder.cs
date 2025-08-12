using Engine.Core.Assets;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

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

    private MaterialInstance? _materialInstance;
    public Material Material
    {
        get => _materialInstance?.Material ?? MaterialAssetManager.FallbackMaterial;
        set => _materialInstance = value.Instantiate();
    }
    public MaterialInstance MaterialInstance
    {
        get => _materialInstance ?? MaterialAssetManager.FallbackMaterialInstance;
        set => _materialInstance = value;
    }
    [Component] public BoundingSphereComponent BoundingSphere;

    private void OnMeshLoaded(AssetVertex[] vertices)
    {
        BoundingSphere.Generate(vertices);
    }

    // public Bgfx.StateFlags RenderFlags { get; set; } = Bgfx.StateFlags.None;
}