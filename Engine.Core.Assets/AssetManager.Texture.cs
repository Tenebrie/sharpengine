using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public class TextureAssetManager : IDisposable
{
    private readonly Dictionary<object, Texture> _cachedTextures = new();
    private static bool _fallbackMeshInitialized = false;
    public static Texture FallbackTexture { get; private set; } = null!;

    private static class FallbackTextureGenerator
    {
        internal static Texture Create()
        {
            var bytes = new byte[] { 255,255,255,255 };
            return Texture.CreateFromBytes(bytes, 1, 1);
        }
    }

    internal static void Initialize()
    {
        if (_fallbackMeshInitialized)
            return;
        FallbackTexture = FallbackTextureGenerator.Create();
        _fallbackMeshInitialized = true;
    }
    
    public bool TryGet(string key, [MaybeNullWhen(false)] out Texture texture)
    {
        return _cachedTextures.TryGetValue(key, out texture);
    }
    
    public void Put(string path, Texture texture)
    {
        if (_cachedTextures.TryGetValue(path, out _))
            throw new InvalidOperationException("Texture already exists");
        _cachedTextures[path] = texture;
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (_fallbackMeshInitialized)
            FallbackTexture.Dispose();
        foreach (var texture in _cachedTextures.Values)
            texture.Dispose();
        _cachedTextures.Clear();
    }
    ~TextureAssetManager() => Dispose();
}
