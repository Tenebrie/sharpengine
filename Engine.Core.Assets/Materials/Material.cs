using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;

namespace Engine.Core.Assets.Materials;

public class Material(MaterialPipeline pipeline, Dictionary<string, IBuffer> constantBuffers) : IDisposable
{
    public MaterialPipeline Pipeline => pipeline;
    public Dictionary<string, IBuffer> ConstantBuffers => constantBuffers;
    
    public void UpdateConstantBuffer<T>(string name, T data) where T : unmanaged
    {
        if (!ConstantBuffers.TryGetValue(name, out var buffer))
            throw new KeyNotFoundException($"Constant buffer '{name}' not found in material.");
        
        var map = RenderContext.Current.DeviceContext.MapBuffer<T>(buffer, MapType.Write, MapFlags.Discard);
        map[0] = data;
        RenderContext.Current.DeviceContext.UnmapBuffer(buffer, MapType.Write);
    }

    public MaterialInstance Instantiate()
    {
        var instance = InstantiateWithoutCache();
        AssetManager.Shared.Materials.RegisterInstance(instance);
        return instance;
    }

    public MaterialInstance InstantiateWithoutCache() => new(this);

    /**
     * Creates a simple material from a shader path.
     * The path is relative to the Assets directory, e.g. "Shaders/ShaderName" matches "{projectRoot}/Assets/Shaders/ShaderName.{ext}".
     */
    public static Material CreateFromDisk(string shaderPath)
    {
        if (AssetManager.Shared.Materials.TryGet(shaderPath, out var material))
            return material;
        var newMaterial = MaterialBuilder.BeginFromFilesystem(shaderPath).Compile();
        AssetManager.Shared.Materials.Put(shaderPath, newMaterial);
        return newMaterial;
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