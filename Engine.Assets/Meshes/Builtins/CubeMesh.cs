using System.Drawing;
using Engine.Assets.Loaders;
using Vector2 = Engine.Core.Common.Vector2;
using Vector3 = Engine.Core.Common.Vector3;

namespace Engine.Assets.Meshes.Builtins;

public class CubeMesh
{
    public static CubeMesh Instance { get; private set; } = new();

    public StaticMesh Mesh;

    public void Load()
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

        ushort[] indices =
        [
            0,1,2,  2,3,0,
            5,4,7,  7,6,5,
            4,0,3,  3,7,4,
            1,5,6,  6,2,1,
            3,2,6,  6,7,3,
            4,5,1,  1,0,4 
        ];

        Mesh = StaticMesh.CreateFromMemory(verts, indices, WindingOrder.Ccw);
    }
}