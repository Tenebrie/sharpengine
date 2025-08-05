using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;
public partial class AssetManager
{
    public static MaterialInstance InstantiateMaterial(string path) => Instance.Materials.InstantiateMaterial(path);
    public static MaterialInstance InstantiateMaterial(object key) => Instance.Materials.InstantiateMaterial(key);
    public static MaterialInstance InstantiateMaterial(Material material) => MaterialAssetManager.InstantiateMaterial(material);

    public static void SubmitMaterial(object key, Material material) => Instance.Materials.SubmitMaterial(key, material);
}


public class MaterialAssetManager
{
    private readonly Dictionary<object, Material> _cachedMaterials = new();
    
    public MaterialInstance InstantiateMaterial(string path)
    {
        if (_cachedMaterials.TryGetValue(path, out var material))
            return new MaterialInstance(material);
        
        material = Material.CreateFromDisk(path);
        _cachedMaterials[path] = material;
        return new MaterialInstance(material);
    }
    public MaterialInstance InstantiateMaterial(object key)
    {
        if (_cachedMaterials.TryGetValue(key, out var material))
            return new MaterialInstance(material);
        
        throw new InvalidOperationException($"Material {key} not found");
    }
    public static MaterialInstance InstantiateMaterial(Material material)
    {
        return new MaterialInstance(material);
    }

    public void SubmitMaterial(object key, Material material)
    {
        if (_cachedMaterials.TryGetValue(key, out _))
            return;
        
        _cachedMaterials[key] = material;
    }
}
