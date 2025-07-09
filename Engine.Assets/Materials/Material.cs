using Engine.Codegen.Bgfx.Unsafe;
using System.IO;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class Material
{
    public ProgramHandle Program { get; protected set; }
    protected ShaderHandle FragShader;
    protected ShaderHandle VertShader;

    protected static unsafe ShaderHandle LoadShader(string path)
    {
        // Read the whole compiled .bin file.
        var data = File.ReadAllBytes(path);
        fixed (byte* ptr = data)
        {
            // bgfx has a convenience overload: Copy(byte[]) returns a bgfx::Memory*.
            var mem = copy(ptr, (uint)data.Length);                  // allocates & copies in one call

            // Create the shader; bgfx takes ownership of 'mem' immediately.
            var sh = create_shader(mem);

            // Sanity-check: in bgfx handles, index 0xFFFF means "invalid".
            if (sh.idx == ushort.MaxValue)
                throw new InvalidOperationException($"Shader '{path}' failed to load.");

            return sh;
        }
    }
}