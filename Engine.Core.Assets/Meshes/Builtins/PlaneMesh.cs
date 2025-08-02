using Engine.Core.Assets.Loaders;
using Engine.Core.Common;

namespace Engine.Core.Assets.Meshes.Builtins;

public static class PlaneMesh
{
    public static StaticMesh Create(float width = 1f, float height = 1f, int segmentsX = 1, int segmentsY = 1)
    {
        var mesh = new StaticMesh();
        var verts = new AssetVertex[(segmentsX + 1) * (segmentsY + 1)];
        var indices = new ushort[segmentsX * segmentsY * 6];

        for (int y = 0; y <= segmentsY; y++)
        {
            for (int x = 0; x <= segmentsX; x++)
            {
                var u = (float)x / segmentsX;
                var v = (float)y / segmentsY;
                verts[y * (segmentsX + 1) + x] = new AssetVertex
                {
                    Position = new Vector3(x * width / segmentsX - width / 2, 0, y * height / segmentsY - height / 2),
                    TexCoord = new Vector2(u, v)
                };
            }
        }

        for (int y = 0; y < segmentsY; y++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int baseIndex = (y * segmentsX + x) * 6;
                int vertexIndex = y * (segmentsX + 1) + x;

                indices[baseIndex]     = (ushort)(vertexIndex);
                indices[baseIndex + 1] = (ushort)(vertexIndex + segmentsX + 1);
                indices[baseIndex + 2] = (ushort)(vertexIndex + 1);
                
                indices[baseIndex + 3] = (ushort)(vertexIndex + segmentsX + 1);
                indices[baseIndex + 4] = (ushort)(vertexIndex + segmentsX + 2);
                indices[baseIndex + 5] = (ushort)(vertexIndex + 1);
            }
        }

        mesh = StaticMesh.CreateFromMemory(verts, indices);
        return mesh;
    }
    
    public static AssetVertex[] CreateVerts(float width = 1f, float height = 1f, int segmentsX = 1, int segmentsY = 1)
    {
        var verts = new AssetVertex[(segmentsX + 1) * (segmentsY + 1)];

        for (int y = 0; y <= segmentsY; y++)
        {
            for (int x = 0; x <= segmentsX; x++)
            {
                var u = (float)x / segmentsX;
                var v = (float)y / segmentsY;
                verts[y * (segmentsX + 1) + x] = new AssetVertex
                {
                    Position = new Vector3(x * width / segmentsX - width / 2, 0, y * height / segmentsY - height / 2),
                    TexCoord = new Vector2(u, v)
                };
            }
        }

        return verts;
    }
}