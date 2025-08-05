using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Meshes;

namespace Engine.Core.Assets;

public partial class AssetManager
{
    public static bool HasMesh(string path) => Instance.Meshes.HasMesh(path);
    public static void PutMesh(string path, StaticMesh mesh) => Instance.Meshes.PutMesh(path, mesh);
    public static StaticMesh LoadMesh(string path) => Instance.Meshes.LoadMesh(path);
}

public class MeshAssetManager
{
    private readonly Dictionary<object, StaticMesh> _cachedMeshes = new();
    
    public bool HasMesh(string path)
    {
        return _cachedMeshes.ContainsKey(path);
    }
    
    public void PutMesh(string path, StaticMesh mesh)
    {
        if (_cachedMeshes.TryGetValue(path, out _))
            return;
        
        _cachedMeshes[path] = mesh;
    }
    public StaticMesh LoadMesh(string path)
    {
        if (_cachedMeshes.TryGetValue(path, out var mesh))
            return mesh;
        
        ObjMeshLoader.LoadObj(path, out var vertices, out var indices);
        mesh = StaticMesh.CreateFromMemory(vertices, indices);
        _cachedMeshes[path] = mesh;
        return mesh;
    }
}
