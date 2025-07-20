using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class Material
{
    // Shaders
    public ProgramHandle Program { get; }

    // Textures
    protected Texture? Texture;
    public UniformHandle DiffuseTextureHandle = create_uniform("s_diffuse", UniformType.Sampler, 1);

    protected Material(string shaderPath)
    {
        var vertShader = LoadShader("bin/" + shaderPath + ".vert.bin");
        var fragShader = LoadShader("bin/" + shaderPath + ".frag.bin");
        Program = CreateProgram(vertShader, fragShader);
    }
    
    public void LoadTexture(string texturePath)
    {
        Texture?.Dispose();
        Texture = Texture.Load(texturePath);
    } 

    public void BindTexture()
    {
        if (Texture == null)
            return;
        
        set_texture(0, DiffuseTextureHandle, Texture.Handle, uint.MaxValue);
    }

    private static unsafe ShaderHandle LoadShader(string path)
    {
        // Path to compiled binary.
        var data = File.ReadAllBytes(path);
        fixed (byte* ptr = data)
        {
            var mem = copy(ptr, (uint)data.Length);
            var sh = create_shader(mem);
            if (sh.idx == ushort.MaxValue)
                throw new InvalidOperationException($"Shader '{path}' failed to load.");
            return sh;
        }
    }

    private static ProgramHandle CreateProgram(ShaderHandle vert, ShaderHandle frag, bool destroyShaders = true)
    {
        var program = create_program(vert, frag, destroyShaders);
        if (program.idx == ushort.MaxValue)
            throw new InvalidOperationException("Program creation failed.");
        return program;
    }
}