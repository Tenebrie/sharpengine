namespace Engine.Core.Assets;

public partial class AssetManager
{
    private static AssetManager Instance { get; } = new();
    private MaterialAssetManager Materials { get; } = new();
    private MeshAssetManager Meshes { get; } = new();
    public static PreparedAssetManager Prepared { get; } = new();
    private TextureAssetManager Textures { get; } = new();
}
