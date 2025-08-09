using System.Drawing;
using Engine.Core.Common;
using static Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material)
{
    private Texture? _texture;
    public Vector4Float TintColor = Vector4.One.Downgrade();

    public ProgramHandle Program => material.Program;

    public MaterialInstance LoadTexture(Texture texture)
    {
        _texture = texture;
        return this;
    }

    public MaterialInstance SetTintColor(Color color, double multiplier = 1.0)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var a = color.A / 255f;
        TintColor = (new Vector4(r, g, b, a) * multiplier).Downgrade();
        return this;
    }
    
    public MaterialInstance SetOpacity(double opacity)
    {
        TintColor.W = (float)Math.Clamp(opacity, 0.0, 1.0);
        return this;
    }

    public unsafe void ApplyForRendering(Encoder* encoder = null)
    {
        var handle = _texture?.Handle ?? NativeTexture.Invalid;
        SetTexture(encoder, 0, material.DiffuseTextureHandle, handle, SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic);
        // SetUniform(encoder, material.TintColorHandle, TintColor);
    }
}