using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Meshes;

namespace Engine.Core.Assets;

public class MeshAssetManager : IDisposable
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
        
        ObjMeshLoader.LoadObj(Path.Combine("Assets", path), out var vertices, out var indices);
        mesh = StaticMesh.CreateFromMemory(vertices, indices);
        return mesh;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var mesh in _cachedMeshes.Values)
        {
            mesh.Dispose();
        }
        _cachedMeshes.Clear();
    }

    ~MeshAssetManager() => Dispose();
}
