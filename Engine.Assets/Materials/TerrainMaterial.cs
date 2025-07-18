using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Assets.Materials;

public class TerrainMaterial : Material
{
    public TerrainMaterial()
    {
        FragShader = LoadShader("bin/terrain.frag.bin");
        if (FragShader.idx == ushort.MaxValue)
        {
            throw new InvalidOperationException("Fragment shader failed to load.");
        }
        VertShader = LoadShader("bin/terrain.vert.bin");
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