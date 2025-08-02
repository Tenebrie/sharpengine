using System.Drawing;
using System.Globalization;
using Engine.Core.Common;

namespace Engine.Core.Assets.Loaders
{
    public struct AssetVertex(Vector3 position, Vector2 texCoord, Vector3 normal, Color vertexColor)
    {
        public Vector3 Position = position;
        public Vector2 TexCoord = texCoord;
        public Vector3 Normal = normal;
        public Color VertexColor = vertexColor;

        public AssetVertex() : this(Vector3.Zero, Vector2.Zero, Vector3.Zero, Color.Aqua) {}
    }

    public static class ObjMeshLoader
    {
        public static void LoadObj(string path, out AssetVertex[] vertices, out ushort[] indices)
        {
            var posList   = new List<Vector3>();
            var uvList    = new List<Vector2>();
            var normList  = new List<Vector3>();
            var colorList = new List<Vector3>();

            // Use a tuple as the key instead of the raw token string so we can normalize
            var vertDict  = new Dictionary<(int vi, int ti, int ni), ushort>();
            var vertList  = new List<AssetVertex>();
            var idxList   = new List<ushort>();

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) 
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "v":
                    {
                        float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float z = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        posList.Add(new Vector3(x, y, z));

                        if (parts.Length >= 7)
                        {
                            float r = float.Parse(parts[4], CultureInfo.InvariantCulture);
                            float g = float.Parse(parts[5], CultureInfo.InvariantCulture);
                            float b = float.Parse(parts[6], CultureInfo.InvariantCulture);
                            colorList.Add(new Vector3(r, g, b));
                        }
                        else
                        {
                            colorList.Add(Vector3.One);
                        }
                        break;
                    }
                    case "vt":
                    {
                        // vt may contain 1, 2 or 3 components (u[, v[, w]])
                        float u = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float v = parts.Length > 2 ? float.Parse(parts[2], CultureInfo.InvariantCulture) : 0f;
                        // Ignore w if present
                        uvList.Add(new Vector2(u, v));
                        break;
                    }
                    case "vn":
                    {
                        float nx = float.Parse(parts[1], CultureInfo.InvariantCulture);
                        float ny = float.Parse(parts[2], CultureInfo.InvariantCulture);
                        float nz = float.Parse(parts[3], CultureInfo.InvariantCulture);
                        normList.Add(new Vector3(nx, ny, nz));
                        break;
                    }
                    case "f":
                    {
                        // parts[1..] are the face vertices – could be 3+ entries
                        // Triangulate fan: (0, i-1, i)
                        for (int i = 2; i < parts.Length - 1; i++)
                        {
                            AddFaceVert(parts[1], posList, uvList, normList, colorList, vertDict, vertList, idxList);
                            AddFaceVert(parts[i], posList, uvList, normList, colorList, vertDict, vertList, idxList);
                            AddFaceVert(parts[i + 1], posList, uvList, normList, colorList, vertDict, vertList, idxList);
                        }
                        break;
                    }
                }
            }

            vertices = vertList.ToArray();
            indices  = idxList.ToArray();
        }

        private static void AddFaceVert(
            string token,
            List<Vector3> posList,
            List<Vector2> uvList,
            List<Vector3> normList,
            List<Vector3> colorList,
            Dictionary<(int vi, int ti, int ni), ushort> vertDict,
            List<AssetVertex> vertList,
            List<ushort> idxList)
        {
            // token like "v/vt/vn", "v//vn", "v/vt", or "v"
            var comps = token.Split('/');
            int vi = ResolveIndex(comps[0], posList.Count);
            int ti = (comps.Length > 1 && comps[1].Length > 0) ? ResolveIndex(comps[1], uvList.Count)   : -1;
            int ni = (comps.Length > 2 && comps[2].Length > 0) ? ResolveIndex(comps[2], normList.Count) : -1;

            var key = (vi, ti, ni);
            if (!vertDict.TryGetValue(key, out ushort idx))
            {
                if (vertList.Count >= ushort.MaxValue)
                    throw new InvalidOperationException("OBJ has more than 65,535 unique vertices. Split the mesh or use multiple draw calls.");

                var vtx = new AssetVertex
                {
                    Position    = posList[vi],
                    TexCoord    = ti >= 0 ? uvList[ti]   : Vector2.Zero,
                    Normal      = ni >= 0 ? normList[ni] : Vector3.Zero,
                    VertexColor = Color.FromArgb(
                        255, 
                        (int)(colorList[vi][0] * 255),
                        (int)(colorList[vi][1] * 255),
                        (int)(colorList[vi][2] * 255)
                    )
                };

                idx = (ushort)vertList.Count;
                vertDict[key] = idx;
                vertList.Add(vtx);
            }

            idxList.Add(idx);
        }

        private static int ResolveIndex(string s, int count)
        {
            int idx = int.Parse(s, CultureInfo.InvariantCulture);
            return idx > 0 ? idx - 1 : count + idx; // negative indices count from end
        }
    }
}
