using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public class TextureAssetManager : IDisposable
{
    private readonly Dictionary<object, Texture> _cachedTextures = new();
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var texture in _cachedTextures.Values)
        {
            texture.Dispose();
        }
        _cachedTextures.Clear();
    }
    ~TextureAssetManager() => Dispose();
}
