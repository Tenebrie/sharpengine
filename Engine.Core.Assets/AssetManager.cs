using System.Reflection;

namespace Engine.Core.Assets;

public partial class AssetManager : IDisposable
{
    public MaterialAssetManager Materials { get; } = new();
    public MeshAssetManager Meshes { get; } = new();
    public TextureAssetManager Textures { get; } = new();
    
    /// <summary>
    /// [EngineInternal]
    /// Gets the shared asset manager for the current assembly.
    /// From userland, `Assembly.GetExecutingAssembly()` is fine.
    /// For engine code, use `Assembly.GetCallingAssembly()` in the first function called from the userland.
    /// </summary>
    public static AssetManager Shared(Assembly assembly) => AssemblyAssetManager.GetAssetManager(assembly);

    public void Initialize()
    {
        MaterialAssetManager.Initialize();
        MeshAssetManager.Initialize();
        // TextureAssetManager.Initialize();
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
        if (AssetManagers.TryGetValue(assembly.GetName().ToString(), out var assetManager))
            return assetManager;

        assetManager = new AssetManager();
        assetManager.Initialize();
        AssetManagers[assembly.GetName().ToString()] = assetManager;
        return assetManager;
    }
    
    public static void DisposeAll()
    {
        foreach (var assetManager in AssetManagers.Values)
        {
            assetManager.Dispose();
        }
        AssetManagers.Clear();
    }
}
