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
        get => _mesh ?? throw new InvalidOperationException("Mesh is not set.");
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
        get => _material ?? throw new InvalidOperationException("Material is not set.");
        set => _material = value;
    }
    [Component] public BoundingSphereComponent BoundingSphere;
    
    private void OnMeshLoaded(AssetVertex[] vertices)
    {
        BoundingSphere.Generate(vertices);
    }
}