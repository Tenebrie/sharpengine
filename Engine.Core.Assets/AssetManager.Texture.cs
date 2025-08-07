using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public class TextureAssetManager : IDisposable
{
    private readonly Dictionary<object, Texture> _cachedTextures = new();
    
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
        foreach (var texture in _cachedTextures.Values)
            texture.Dispose();
        _cachedTextures.Clear();
    }
    ~TextureAssetManager() => Dispose();
}
