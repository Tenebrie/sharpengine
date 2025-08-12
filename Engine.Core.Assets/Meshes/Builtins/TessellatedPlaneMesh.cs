using System.Reflection;
using Engine.Core.Assets.Loaders;
using Engine.Core.Common;

namespace Engine.Core.Assets.Meshes.Builtins;

public static class TessellatedPlaneMesh
{
    public static StaticMesh Create(float width = 1f, float height = 1f, int segmentsX = 1, int segmentsY = 1)
    {
        var key = $"{width}_{height}_{segmentsX}_{segmentsY}";
        if (AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.TryGet(key, out var mesh))
            return mesh;
        mesh = CreateWithoutCache(width, height, segmentsX, segmentsY);
        AssetManager.AssemblyShared(Assembly.GetCallingAssembly()).Meshes.Put(key, mesh);
        return mesh;
    }

    public static StaticMesh CreateWithoutCache(float width = 1f, float height = 1f, int segmentsX = 1, int segmentsY = 1)
    {
        var verts = CreateVerts(width, height, segmentsX, segmentsY);
        var indices = CreateIndices(segmentsX * segmentsY);
        return StaticMesh.CreateFromMemoryWithoutCache(verts, indices);
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
    public static uint[] CreateIndices(int segmentsX = 1, int segmentsY = 1)
    {
        var indices = new uint[segmentsX * segmentsY * 6];

        for (int y = 0; y < segmentsY; y++)
        {
            for (int x = 0; x < segmentsX; x++)
            {
                int baseIndex = (y * segmentsX + x) * 6;
                int vertexIndex = y * (segmentsX + 1) + x;

                indices[baseIndex] = (uint)(vertexIndex);
                indices[baseIndex + 1] = (uint)(vertexIndex + segmentsX + 1);
                indices[baseIndex + 2] = (uint)(vertexIndex + 1);

                indices[baseIndex + 3] = (uint)(vertexIndex + segmentsX + 1);
                indices[baseIndex + 4] = (uint)(vertexIndex + segmentsX + 2);
                indices[baseIndex + 5] = (uint)(vertexIndex + 1);
            }
        }

        return indices;
    }
}