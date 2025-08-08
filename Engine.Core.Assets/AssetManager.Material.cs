using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public class MaterialAssetManager : IDisposable
{
    private readonly Dictionary<object, Material> _cachedMaterials = new();
    private static bool _fallbackMaterialInitialized = false;
    private static Material _fallbackMaterial = null!;
    public static MaterialInstance FallbackMaterial { get; private set; } = null!;

    private static class FallbackMaterialGenerator
    {
        internal static Material Create()
        {
            return MaterialBuilder.Begin("FallbackMaterial")
                .SetCacheAutomatically(false)
                .SetTintColor(System.Drawing.Color.Purple)
                .SetSamplingTexture(false)
                .Compile();
        }
    }
    
    internal static void Initialize()
    {
        if (_fallbackMaterialInitialized)
            return;
        _fallbackMaterial = FallbackMaterialGenerator.Create();
        FallbackMaterial = _fallbackMaterial.InstantiateWithoutCache();
        _fallbackMaterialInitialized = true;
    }
    
    public bool TryGet(object key, [MaybeNullWhen(false)] out Material material)
    {
        return _cachedMaterials.TryGetValue(key, out material);
    }

    public void Put(object key, Material material)
    {
        if (_cachedMaterials.TryGetValue(key, out _))
            throw new InvalidOperationException($"Material {key} already exists");
        _cachedMaterials[key] = material;
    }

    public void RegisterInstance(MaterialInstance _)
    {
        // NOOP
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var material in _cachedMaterials.Values)
            material.Dispose();

        if (_fallbackMaterialInitialized)
        {
            Console.WriteLine("Disposing fallback material");
            _fallbackMaterial.Dispose();
        }
        _fallbackMaterialInitialized = false;
        _cachedMaterials.Clear();
    }
    
    ~MaterialAssetManager() => Dispose();
}
