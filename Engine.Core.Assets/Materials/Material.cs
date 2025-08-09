using System.Reflection;
using static Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Materials;

public class Material : IDisposable
{
    public ShaderHandle VertexShader { get; private set; }
    public ShaderHandle FragmentShader { get; private set; }
    public ProgramHandle Program { get; }
    public UniformHandle DiffuseTextureHandle { get; }
    // public UniformHandle TintColorHandle { get; }

    private Material(ProgramHandle program, ShaderHandle vertShader, ShaderHandle fragShader)
    {
        VertexShader = vertShader;
        FragmentShader = fragShader;
        Program = program;
        DiffuseTextureHandle = create_uniform("s_diffuse", UniformType.Sampler, 1);
        // TintColorHandle = create_uniform("u_tintColor", UniformType.Vec4, 1);
    }
    protected Material(string shaderPath)
    {
        var vertShader = LoadShader("Compiled/Shaders/" + shaderPath + ".vert.bin");
        var fragShader = LoadShader("Compiled/Shaders/" + shaderPath + ".frag.bin");
        VertexShader = vertShader;
        FragmentShader = fragShader;
        Program = CreateProgram(vertShader, fragShader);
        DiffuseTextureHandle = create_uniform("s_diffuse", UniformType.Sampler, 1);
        // TintColorHandle = create_uniform("u_tintColor", UniformType.Vec4, 1);
    }

    public MaterialInstance Instantiate()
    {
        var instance = InstantiateWithoutCache();
        AssetManager.Shared(Assembly.GetCallingAssembly()).Materials.RegisterInstance(instance);
        return instance;
    }

    public MaterialInstance InstantiateWithoutCache() => new(this);

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
        if (program.idx == ushort.MaxValue || !program.Valid)
            throw new InvalidOperationException("Program creation failed.");
        return program;
    }

    public static Material CreateFromDisk(string shaderPath)
    {
        if (AssetManager.Shared(Assembly.GetCallingAssembly()).Materials.TryGet(shaderPath, out var material))
            return material;
        var newMaterial = new Material(shaderPath);
        AssetManager.Shared(Assembly.GetCallingAssembly()).Materials.Put(shaderPath, newMaterial);
        return newMaterial;
    }

    internal static Material CreateFromGenerated(string fullShaderPath)
    {
        var vertShader = LoadShader(fullShaderPath + ".vert.bin");
        var fragShader = LoadShader(fullShaderPath + ".frag.bin");
        var program = CreateProgram(vertShader, fragShader);
        return new Material(program, vertShader, fragShader);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        destroy_program(Program);
        destroy_uniform(DiffuseTextureHandle);
        // destroy_uniform(TintColorHandle);
    }

    ~Material() => Dispose();
}