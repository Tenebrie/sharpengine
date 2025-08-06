using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Meshes;

namespace Engine.Core.Assets;

public partial class AssetManager
{
    public static bool HasMesh(string path) => Meshes.Has(path);
    public static void PutMesh(string path, StaticMesh mesh) => Meshes.Put(path, mesh);
    public static StaticMesh LoadMesh(string path) => Meshes.LoadFromDisk(path);
}

public class MeshAssetManager
{
    private readonly Dictionary<object, StaticMesh> _cachedMeshes = new();
    
    public bool Has(string path)
    {
        return _cachedMeshes.ContainsKey(path);
    }
    
    public bool TryGet(string key, [MaybeNullWhen(false)] out StaticMesh mesh)
    {
        return _cachedMeshes.TryGetValue(key, out mesh);
    }
    
    public void Put(string path, StaticMesh mesh)
    {
        if (_cachedMeshes.TryGetValue(path, out _))
            return;
        
        Console.WriteLine("Putting mesh into cache: " + path);
        _cachedMeshes[path] = mesh;
    }
    public StaticMesh LoadFromDisk(string path)
    {
        if (_cachedMeshes.TryGetValue(path, out var mesh))
            return mesh;
        
        ObjMeshLoader.LoadObj(path, out var vertices, out var indices);
        mesh = StaticMesh.CreateFromMemory(vertices, indices);
        return mesh;
    }
    
    public void Shutdown()
    {
        foreach (var mesh in _cachedMeshes.Values)
        {
            mesh.Dispose();
        }
        _cachedMeshes.Clear();
    }
}
