using System.Reflection;

namespace Engine.Core.Assets;

public enum AssetType
{
    Material,
    Mesh,
    Texture
}

public partial class AssetManager : IDisposable
{
    public MaterialAssetManager Materials { get; } = new();
    public MeshAssetManager Meshes { get; } = new();
    public TextureAssetManager Textures { get; } = new();
    public static AssetManager Shared(Assembly assembly) => AssemblyAssetManager.GetAssetManager(assembly);

    public void Initialize()
    {
        MaterialAssetManager.Initialize();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Materials.Dispose();
        Meshes.Dispose();
        Textures.Dispose();
    }
    
    ~AssetManager() => Dispose();
}

public static class AssemblyAssetManager
{
    private static Dictionary<string, AssetManager> AssetManagers { get; } = new();
    public static AssetManager GetAssetManager(Assembly assembly)
    {
        Console.WriteLine(assembly.GetName());
        if (AssetManagers.TryGetValue(assembly.GetName().ToString(), out var assetManager))
            return assetManager;

        assetManager = new AssetManager();
        assetManager.Initialize();
        AssetManagers[assembly.GetName().ToString()] = assetManager;
        return assetManager;
    }
}
