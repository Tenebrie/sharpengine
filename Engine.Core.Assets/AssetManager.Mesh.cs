using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;

namespace Engine.Core.Assets;

public class MeshAssetManager : IDisposable
{
    private readonly Dictionary<object, StaticMesh> _cachedMeshes = new();
    private static bool _fallbackMeshInitialized = false;
    public static StaticMesh FallbackMesh { get; private set; } = null!;

    private static class FallbackMeshGenerator
    {
        internal static StaticMesh Create()
        {
            return CubeMesh.Create();
        }
    }

    internal static void Initialize()
    {
        if (_fallbackMeshInitialized)
            return;
        FallbackMesh = FallbackMeshGenerator.Create();
        _fallbackMeshInitialized = true;
    }

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
            throw new InvalidOperationException("Mesh already exists");
        _cachedMeshes[path] = mesh;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var mesh in _cachedMeshes.Values)
            mesh.Dispose();
        if (_fallbackMeshInitialized)
            FallbackMesh.Dispose();
        _fallbackMeshInitialized = false;
        _cachedMeshes.Clear();
    }

    ~MeshAssetManager() => Dispose();
}
