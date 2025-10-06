using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material) : IDisposable
{
    public Material Material = material;
    public ITextureView? RemoteTextureView; // Optional override for the material's texture
    public Vector4Float Tint = Vector4.One.Downgrade();
    public Vector2Float UvOffset = Vector2.Zero.Downgrade();
    public Vector2Float UvScale = Vector2.One.Downgrade();
    
    public MaterialInstance SetTintColor(Color color, double multiplier = 1.0)
    {
        Tint = (color.ToVector4() * multiplier).Downgrade();    
        return this;
    }
    public MaterialInstance SetOpacity(double opacity)
    {
        Tint.W = (float)Math.Clamp(opacity, 0.0, 1.0);
        return this;
    }
    
    public MaterialInstance SetUvOffset(Vector2 offset)
    {
        UvOffset = offset.Downgrade();
        return this;
    }
    public MaterialInstance SetUvScale(double scale)
    {
        UvScale = new Vector2(scale, scale).Downgrade();
        return this;
    }
    public MaterialInstance SetUvScale(Vector2 scale)
    {
        UvScale = scale.Downgrade();
        return this;
    }

    public MaterialInstance SetRemoteTextureView(ITextureView textureView)
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
        
        var textureToBind = Material.Texture ?? TextureAssetManager.FallbackTexture;
        var textureSrv = textureToBind.GetDefaultView();
        sampler.Set(textureSrv, SetShaderResourceFlags.None);
    }
    
    private void BindConstantBuffers(IShaderResourceBinding srb)
    {
        foreach (var (name, buffer) in Material.ConstantBuffers)
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
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Material.RemoveInstance(this);
    }
}