using System.Drawing;
using System.Reflection;
using Engine.Core.Assets.Loaders;
using Vector2 = Engine.Core.Common.Vector2;
using Vector3 = Engine.Core.Common.Vector3;

namespace Engine.Core.Assets.Meshes.Builtins;

public class CubeMesh : StaticMesh
{
    public static CubeMesh Instance { get; private set; } = new();

    public StaticMesh Mesh = null!;

    public void Load()
    {
        if (AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.TryGet("Generated/ColorCube", out var existingMesh))
        {
            Mesh = existingMesh;
            return;
        }

        AssetVertex[] verts =
        [
            new(new Vector3(-1, -1, -1), Vector2.Zero, Vector3.One, Color.Red),
            new(new Vector3( 1, -1, -1), Vector2.Zero, Vector3.One, Color.Green),
            new(new Vector3( 1,  1, -1), Vector2.Zero, Vector3.One, Color.Yellow),
            new(new Vector3(-1,  1, -1), Vector2.Zero, Vector3.One, Color.Blue),
            new(new Vector3(-1, -1,  1), Vector2.Zero, Vector3.One, Color.Cyan),
            new(new Vector3( 1, -1,  1), Vector2.Zero, Vector3.One, Color.Magenta),
            new(new Vector3( 1,  1,  1), Vector2.Zero, Vector3.One, Color.White),
            new(new Vector3(-1,  1,  1), Vector2.Zero, Vector3.One, Color.Gray)
        ];

        uint[] indices =
        [
            0,1,2,  2,3,0,
            5,4,7,  7,6,5,
            4,0,3,  3,7,4,
            1,5,6,  6,2,1,
            3,2,6,  6,7,3,
            4,5,1,  1,0,4
        ];

        var mesh = CreateFromMemoryWithoutCache(verts, indices, WindingOrder.Ccw);
        AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.Put("Generated/ColorCube", mesh);
        Mesh = mesh;
    }

    public static StaticMesh Create()
    {
        AssetVertex[] verts =
        [
            new(new Vector3(-1, -1, -1), Vector2.Zero, Vector3.One, Color.Red),
            new(new Vector3( 1, -1, -1), Vector2.Zero, Vector3.One, Color.Green),
            new(new Vector3( 1,  1, -1), Vector2.Zero, Vector3.One, Color.Yellow),
            new(new Vector3(-1,  1, -1), Vector2.Zero, Vector3.One, Color.Blue),
            new(new Vector3(-1, -1,  1), Vector2.Zero, Vector3.One, Color.Cyan),
            new(new Vector3( 1, -1,  1), Vector2.Zero, Vector3.One, Color.Magenta),
            new(new Vector3( 1,  1,  1), Vector2.Zero, Vector3.One, Color.White),
            new(new Vector3(-1,  1,  1), Vector2.Zero, Vector3.One, Color.Gray)
        ];

        uint[] indices =
        [
            0,1,2,  2,3,0,
            5,4,7,  7,6,5,
            4,0,3,  3,7,4,
            1,5,6,  6,2,1,
            3,2,6,  6,7,3,
            4,5,1,  1,0,4
        ];

        return CreateFromMemoryWithoutCache(verts, indices, WindingOrder.Ccw);
    }
}