namespace Engine.Core.Assets;

public partial class AssetManager
{
    private static AssetManager Instance { get; } = new();
    private MaterialAssetManager Materials { get; } = new();
    public static MeshAssetManager Meshes { get; } = new();
    private TextureAssetManager Textures { get; } = new();
    public static AssetFinalizerManager Finalizers { get; } = new();
    
    public static void Shutdown()
    {
        Instance.Materials.Shutdown();
        Meshes.Shutdown();
        Instance.Textures.Shutdown();
        Finalizers.Invoke();
    }
}
