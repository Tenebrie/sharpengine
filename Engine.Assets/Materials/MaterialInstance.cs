using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class MaterialInstance(Material material)
{
    protected Texture? Texture;
    public UniformHandle DiffuseTextureHandle = create_uniform("s_diffuse", UniformType.Sampler, 1);

    public ProgramHandle Program => material.Program;
    
    public void LoadTexture(string texturePath)
    {
        Texture = AssetManager.LoadTexture(texturePath);
    }

    public void BindTexture()
    {
        if (Texture == null)
            return;
        
        set_texture(0, DiffuseTextureHandle, Texture.Handle, (uint)(SamplerFlags.MinAnisotropic | SamplerFlags.MagAnisotropic));
    }
}