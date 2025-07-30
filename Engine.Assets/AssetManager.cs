using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Codegen.Bgfx.Unsafe;

namespace Engine.Assets;

public class AssetManager
{
    public static AssetManager Instance { get; } = new();
    
    public Dictionary<string, StaticMesh> CachedMeshes = new();
    public Dictionary<string, Texture> CachedTextures = new();
    public Dictionary<string, Material> CachedMaterials = new();
    
    public static bool HasMesh(string path)
    {
        return Instance.CachedMeshes.ContainsKey(path);
    }
    
    public static void PutMesh(string path, StaticMesh mesh)
    {
        if (Instance.CachedMeshes.TryGetValue(path, out var value))
            value.Dispose();
        
        Instance.CachedMeshes[path] = mesh;
    }
    public static StaticMesh LoadMesh(string path)
    {
        if (Instance.CachedMeshes.TryGetValue(path, out var mesh))
            return mesh;
        
        ObjMeshLoader.LoadObj(path, out var vertices, out var indices);
        mesh = StaticMesh.CreateFromMemory(vertices, indices);
        Instance.CachedMeshes[path] = mesh;
        return mesh;
    }
    
    public static Texture LoadTexture(string path)
    {
        if (Instance.CachedTextures.TryGetValue(path, out var texture))
            return texture;
        
        texture = Texture.CreateFromDisk(path);
        Instance.CachedTextures[path] = texture;
        return texture;
    }
    
    public static MaterialInstance LoadMaterial(string path)
    {
        if (Instance.CachedMaterials.TryGetValue(path, out var material))
            return new MaterialInstance(material);
        
        material = Material.CreateFromDisk(path);
        Instance.CachedMaterials[path] = material;
        return new MaterialInstance(material);
    }
}