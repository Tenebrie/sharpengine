using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Extensions;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material)
{
    public Material Material = material;
    private Texture? _texture;
    public bool SamplingTexture = true;
    public Vector4Float Tint = Vector4.One.Downgrade();
    public Vector2Float UvOffset = Vector2.Zero.Downgrade();
    public Vector2Float UvScale = Vector2.One.Downgrade();
    
    public static RenderContext Context { get; set; }

    public MaterialInstance LoadTexture(Texture texture)
    {
        _texture = texture;
        InvalidateCache();
        return this;
    }

    public MaterialInstance SetSamplingTexture(bool sampling)
    {
        SamplingTexture = sampling;
        InvalidateCache();
        return this;
    }
    
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
    public MaterialInstance SetUvScale(Vector2 scale)
    {
        UvScale = scale.Downgrade();
        return this;
    }

    private readonly Dictionary<IPipelineState, IShaderResourceBinding> _shaderBindingCache = new();

    public IShaderResourceBinding BindMaterial(IPipelineState pipelineState)
    {
        if (_shaderBindingCache.TryGetValue(pipelineState, out var shaderBinding))
            return shaderBinding;
        
        var srb = pipelineState.CreateShaderResourceBinding(true);
        BindTexture(srb);
        _shaderBindingCache[pipelineState] = srb;
        return srb;
    }

    private void BindTexture(IShaderResourceBinding srb)
    {
        var sampler = srb.GetVariableByName(ShaderType.Pixel, ShaderVariable.AlbedoSampler);
        if (sampler is null || !SamplingTexture)
            return;
        var textureToBind = _texture ?? TextureAssetManager.FallbackTexture;
        var textureSrv = textureToBind.GetDefaultView();
        sampler.Set(textureSrv, SetShaderResourceFlags.None);
    }

    private void InvalidateCache()
    {
        foreach (var shaderResourceBinding in _shaderBindingCache)
            shaderResourceBinding.Value.Dispose();
        _shaderBindingCache.Clear();
    }
}