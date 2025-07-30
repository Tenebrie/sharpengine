using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class Material
{
    public ProgramHandle Program { get; }
    
    protected Material(string shaderPath)
    {
        var vertShader = LoadShader("Compiled/Shaders/" + shaderPath + ".vert.bin");
        var fragShader = LoadShader("Compiled/Shaders/" + shaderPath + ".frag.bin");
        Program = CreateProgram(vertShader, fragShader);
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

    public static Material CreateFromDisk(string shaderPath)
    {
        return new Material(shaderPath);
    }
}