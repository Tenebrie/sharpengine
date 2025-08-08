using Engine.Native.Bgfx;
using static Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material)
{
    protected Texture? Texture;

    public ProgramHandle Program => material.Program;
    public UniformHandle DiffuseTextureHandle => material.DiffuseTextureHandle;

    public MaterialInstance LoadTexture(Texture texture)
    {
        Texture = texture;
        return this;
    }

    public void LoadTextureForRendering()
    {
        if (Texture == null)
        {
            SetTexture(0, DiffuseTextureHandle, NativeTexture.Invalid, 0);
            return;
        }

        SetTexture(0, DiffuseTextureHandle, Texture.Handle, SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic);
    }

    public unsafe void LoadTextureForRendering(Encoder* encoder)
    {
        if (Texture == null)
        {
            SetTexture(0, DiffuseTextureHandle, NativeTexture.Invalid, 0);
            return;
        }

        SetTexture(encoder, 0, DiffuseTextureHandle, Texture.Handle, SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic);
    }
}