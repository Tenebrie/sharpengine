using System.Diagnostics;
using Engine.Codegen.Bgfx.Unsafe;
using System.IO;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class UnlitMaterial : Material
{
    public UnlitMaterial()
    {
        FragShader = LoadShader("bin/cube.frag.bin");
        if (FragShader.idx == ushort.MaxValue)
        {
            throw new InvalidOperationException("Fragment shader failed to load.");
        }
        VertShader = LoadShader("bin/cube.vert.bin");
        if (VertShader.idx == ushort.MaxValue)
        {
            throw new InvalidOperationException("Vertex shader failed to load.");
        }
        Program = create_program(VertShader, FragShader, true);
        if (Program.idx == ushort.MaxValue)
        {
            throw new InvalidOperationException("Program failed to load.");
        }
    }
}