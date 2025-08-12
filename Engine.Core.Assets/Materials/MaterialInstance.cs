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
    public Vector4Float Tint = Vector4.One.Downgrade();
    
    public static RenderContext Context { get; set; }

    public MaterialInstance LoadTexture(Texture texture)
    {
        _texture = texture;
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

    private readonly Dictionary<IPipelineState, IShaderResourceBinding> _shaderBindingCache = new();

    public IShaderResourceBinding ProduceResourceBinding(IPipelineState pipelineState)
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
        var textureToBind = _texture ?? TextureAssetManager.FallbackTexture;
        var textureSrv = textureToBind.GetDefaultView();
        srb.GetVariableByName(ShaderType.Pixel, ShaderVariable.AlbedoSampler)?.Set(textureSrv, SetShaderResourceFlags.None);
    }
    
    public void InvalidateCache()
    {
        foreach (var shaderResourceBinding in _shaderBindingCache)
            shaderResourceBinding.Value.Dispose();
        _shaderBindingCache.Clear();
    }
}