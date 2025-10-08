using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Materials;

public class Material(MaterialPipeline pipeline, Texture? texture, Dictionary<string, IBuffer> constantBuffers) : IDisposable
{
    public MaterialPipeline Pipeline => pipeline;
    public Texture? Texture { get; private set; } = texture;
    public ITextureView? RemoteTextureView; // Optional override for the material's texture
    public Dictionary<string, IBuffer> ConstantBuffers => constantBuffers;
    private List<MaterialInstance> Instances { get; } = [];
    
    public void UpdateTexture(Texture texture)
    {
        Texture = texture;
        InvalidateCache();
    }
    
    public void UpdateConstantBuffer<T>(string name, T data) where T : unmanaged
    {
        if (!ConstantBuffers.TryGetValue(name, out var buffer))
            throw new KeyNotFoundException($"Constant buffer '{name}' not found in material.");
        
        RenderThreadTask.Run("Material -> UpdateConstantBuffer", () =>
        {
            var map = RenderContext.Current.ImmediateContext.MapBuffer<T>(buffer, MapType.Write, MapFlags.Discard);
            map[0] = data;
            RenderContext.Current.ImmediateContext.UnmapBuffer(buffer, MapType.Write);
        });
    }
    
    public Material SetRemoteTextureView(ITextureView textureView)
    {
        RemoteTextureView = textureView;
        return this;
    }
    
    private readonly Dictionary<IPipelineState, IShaderResourceBinding> _shaderBindingCache = new();

    public IShaderResourceBinding BindMaterial(IPipelineState pipelineState)
    {
        if (_shaderBindingCache.TryGetValue(pipelineState, out var shaderBinding))
            return shaderBinding;
        
        var srb = pipelineState.CreateShaderResourceBinding(true);
        BindTexture(srb);
        BindConstantBuffers(srb);
        _shaderBindingCache[pipelineState] = srb;
        return srb;
    }

    private void BindTexture(IShaderResourceBinding srb)
    {
        var sampler = srb.GetVariableByName(ShaderType.Pixel, ShaderVariable.AlbedoSampler);
        if (sampler is null)
            return;
        
        if (RemoteTextureView is not null)
        {
            sampler.Set(RemoteTextureView, SetShaderResourceFlags.None);
            return;
        }
        
        var textureToBind = Texture ?? TextureAssetManager.FallbackTexture;
        var textureSrv = textureToBind.GetDefaultView();
        sampler.Set(textureSrv, SetShaderResourceFlags.None);
    }
    
    private void BindConstantBuffers(IShaderResourceBinding srb)
    {
        foreach (var (name, buffer) in ConstantBuffers)
        {
            srb.GetVariableByName(ShaderType.Vertex, name)?.Set(buffer, SetShaderResourceFlags.None);
            srb.GetVariableByName(ShaderType.Pixel, name)?.Set(buffer, SetShaderResourceFlags.None);
        }
    }

    public void InvalidateCache()
    {
        foreach (var shaderResourceBinding in _shaderBindingCache)
            shaderResourceBinding.Value.Dispose();
        _shaderBindingCache.Clear();
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