using Engine.Native.Bgfx;
using static Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material) : IDisposable
{
    protected Texture? Texture;

    public ProgramHandle Program => material.Program;
    public UniformHandle DiffuseTextureHandle => material.DiffuseTextureHandle;
    
    public void LoadTexture(string texturePath)
    {
        Texture = AssetManager.LoadTexture(texturePath);
    }

    public void BindTexture()
    {
        if (Texture == null)
        {
            var invalidHandle = new TextureHandle { idx = ushort.MaxValue };
            set_texture(0, DiffuseTextureHandle, invalidHandle, 0);
            return;
        }
        
        set_texture(0, DiffuseTextureHandle, Texture.Handle, (uint)(SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic));
    }
    
    public unsafe void BindTexture(Encoder* encoder)
    {
        if (Texture == null)
        {
            var invalidHandle = new TextureHandle { idx = ushort.MaxValue };
            set_texture(0, DiffuseTextureHandle, invalidHandle, 0);
            return;
        }
        
        encoder_set_texture(encoder, 0, DiffuseTextureHandle, Texture.Handle, (uint)(SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic));
    }
    
    public void Dispose()
    {
        destroy_uniform(DiffuseTextureHandle);
    }
}