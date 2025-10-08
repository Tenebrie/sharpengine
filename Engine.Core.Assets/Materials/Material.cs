using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Materials;

public class Material(MaterialPipeline pipeline, Texture? texture, Dictionary<string, IBuffer> constantBuffers) : IDisposable
{
    public MaterialPipeline Pipeline => pipeline;
    public Texture? Texture { get; private set; } = texture;
    public Dictionary<string, IBuffer> ConstantBuffers => constantBuffers;
    private List<MaterialInstance> Instances { get; } = [];
    
    public void UpdateTexture(Texture texture)
    {
        Texture = texture;
        foreach (var instance in Instances)
            instance.InvalidateCache();
    }
    
    public void UpdateConstantBuffer<T>(string name, T data) where T : unmanaged
    {
        if (!ConstantBuffers.TryGetValue(name, out var buffer))
            throw new KeyNotFoundException($"Constant buffer '{name}' not found in material.");
        
        var map = RenderContext.Current.ImmediateContext.MapBuffer<T>(buffer, MapType.Write, MapFlags.Discard);
        map[0] = data;
        RenderContext.Current.ImmediateContext.UnmapBuffer(buffer, MapType.Write);
    }

    public MaterialInstance Instantiate()
    {
        var instance = InstantiateWithoutCache();
        Instances.Add(instance);
        return instance;
    }

    public MaterialInstance InstantiateWithoutCache() => new(this);

    public void RemoveInstance(MaterialInstance instance)
    {
        Instances.Remove(instance);
    }
    /**
     * Creates a simple material from a shader path.
     * The path is relative to the Assets directory, e.g. "Shaders/ShaderName" matches "{projectRoot}/Assets/Shaders/ShaderName.{ext}".
     */
    public static Material CreateCachedFromDisk(string shaderPath)
    {
        if (AssetManager.Shared.Materials.TryGet(shaderPath, out var material))
            return material;
        var newMaterial = MaterialBuilder.CreateFromDisk(shaderPath).AsSharedMaterial().Compile();
        AssetManager.Shared.Materials.Put(shaderPath, newMaterial);
        return newMaterial;
    }
    
    public void InvalidateInstancesCache()
    {
        foreach (var instance in Instances)
            instance.InvalidateCache();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Pipeline.PixelShader.Dispose();
        Pipeline.VertexShader.Dispose();
        foreach (var buffer in ConstantBuffers.Values)
            buffer.Dispose();
        ConstantBuffers.Clear();
    }
}