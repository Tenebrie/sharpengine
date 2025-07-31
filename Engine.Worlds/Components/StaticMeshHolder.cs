using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Components;

public class StaticMeshHolder : ActorComponent
{
    private StaticMesh? _mesh;
    public StaticMesh Mesh
    {
        get => _mesh ?? throw new InvalidOperationException("Mesh is not set.");
        set
        {
            _mesh?.OnMeshLoaded.Disconnect(OnMeshLoaded);
            _mesh = value;
            _mesh.OnMeshLoaded.Connect(OnMeshLoaded);
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