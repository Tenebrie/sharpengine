using System.IO.Pipelines;
using System.Reflection;

namespace Engine.Core.Assets;

public partial class AssetManager : IDisposable
{
    public MaterialAssetManager Materials { get; } = new();
    public MeshAssetManager Meshes { get; } = new();
    public PipelineAssetManager Pipelines { get; } = new();
    public TextureAssetManager Textures { get; } = new();
    
    /// <summary>
    /// [EngineInternal]
    /// Gets the shared asset manager for the current assembly.
    /// From userland, `Assembly.GetExecutingAssembly()` is fine.
    /// For engine code, use `Assembly.GetCallingAssembly()` in the first function called from the userland.
    /// </summary>
    public static AssetManager Shared => AssemblyAssetManager.GetAssetManager();
    public static AssetManager AssemblyShared(Assembly assembly) => AssemblyAssetManager.GetAssetManager(Assembly.GetExecutingAssembly());

    public void Initialize()
    {
        MaterialAssetManager.Initialize();
        MeshAssetManager.Initialize();
        TextureAssetManager.Initialize();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Materials.Dispose();
        Meshes.Dispose();
        Pipelines.Dispose();
        Textures.Dispose();
    }
}

public static class AssemblyAssetManager
{
    private static Dictionary<string, AssetManager> AssetManagers { get; } = new();

    public static AssetManager GetAssetManager(Assembly assembly)
    {
        return GetAssetManager(assembly.GetName().ToString());
    }

    public static AssetManager GetAssetManager(string name = "Shared")
    {
        if (AssetManagers.TryGetValue(name, out var assetManager))
            return assetManager;

        assetManager = new AssetManager();
        assetManager.Initialize();
        AssetManagers[name] = assetManager;
        return assetManager;
    }
    
    public static void DisposeAll()
    {
        foreach (var assetManager in AssetManagers.Values)
            assetManager.Dispose();
        AssetManagers.Clear();
    }
}
