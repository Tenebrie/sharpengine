using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public class MaterialAssetManager : IDisposable
{
    private readonly Dictionary<object, Material> _cachedMaterials = new();
    private static bool _fallbackMaterialInitialized = false;
    public static MaterialInstance FallbackMaterial { get; private set; } = null!;

    private static class FallbackMaterialGenerator
    {
        internal static MaterialInstance Create()
        {
            return MaterialBuilder.Begin("FallbackMaterial")
                .SetCacheAutomatically(false)
                .SetTintColor(System.Drawing.Color.White)
                .SetSamplingTexture(false)
                .Compile()
                .Instantiate();
        }
    }
    
    internal static void Initialize()
    {
        if (_fallbackMaterialInitialized)
            return;
        FallbackMaterial = FallbackMaterialGenerator.Create();
        _fallbackMaterialInitialized = true;
    }
    
    public MaterialInstance Instantiate(string path)
    {
        if (_cachedMaterials.TryGetValue(path, out var material))
            return new MaterialInstance(material);
        
        material = Material.CreateFromDisk(path);
        _cachedMaterials[path] = material;
        return new MaterialInstance(material);
    }
    public MaterialInstance Instantiate(object key)
    {
        if (_cachedMaterials.TryGetValue(key, out var material))
            return new MaterialInstance(material);
        
        throw new InvalidOperationException($"Material {key} not found");
    }
    public MaterialInstance Instantiate(Material material)
    {
        if (_cachedMaterials.TryGetValue(material, out _))
            return new MaterialInstance(material);
        _cachedMaterials[material] = material;
        return new MaterialInstance(material);
    }
    
    public bool TryGet(object key, [MaybeNullWhen(false)] out Material material)
    {
        return _cachedMaterials.TryGetValue(key, out material);
    }

    public void Submit(object key, Material material)
    {
        if (_cachedMaterials.TryGetValue(key, out _))
        {
            throw new InvalidOperationException($"Material {key} already exists");
        }
        
        _cachedMaterials[key] = material;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var material in _cachedMaterials.Values)
        {
            material.Dispose();
        }
        _cachedMaterials.Clear();
    }
    
    ~MaterialAssetManager() => Dispose();
}
