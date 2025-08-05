using Engine.Core.Assets.Materials;

namespace Engine.Core.Assets;

public partial class AssetManager
{
    public static Texture LoadTexture(string path) => Instance.Textures.LoadTexture(path);
}

public class TextureAssetManager
{
    private readonly Dictionary<object, Texture> _cachedTextures = new();
    
    public Texture LoadTexture(string path)
    {
        if (_cachedTextures.TryGetValue(path, out var texture))
            return texture;
        
        texture = Texture.CreateFromDisk(path);
        _cachedTextures[path] = texture;
        return texture;
    }
}
