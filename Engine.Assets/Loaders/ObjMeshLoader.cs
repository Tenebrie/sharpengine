using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace Engine.Assets.Loaders;

public struct AssetVertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector3 Normal;
    public Vector3 VertexColor;
}

public class ObjMeshLoader
{
    // Quick & dirty: assumes all faces are triangles and fully specified (v/vt/vn).
    public static void LoadObj(string path, out AssetVertex[] vertices, out ushort[] indices)
    {
        var posList  = new List<Vector3>();
        var uvList   = new List<Vector2>();
        var normList = new List<Vector3>();
        var colorList = new List<Vector3>();
        var vertDict = new Dictionary<string, ushort>(); 
        var vertList = new List<AssetVertex>();
        var idxList  = new List<ushort>();

        foreach (var raw in File.ReadLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) 
                continue;

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case "v":
                    posList.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    ));
                    if (parts.Length >= 7)
                    {
                        var r = float.Parse(parts[4]);
                        var g = float.Parse(parts[5]);
                        var b = float.Parse(parts[6]);
                        colorList.Add(new Vector3(r, g, b));
                    }
                    else
                        colorList.Add(Vector3.One);
                    break;
                case "vt":
                    uvList.Add(new Vector2(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture)
                    ));
                    break;
                case "vn":
                    normList.Add(new Vector3(
                        float.Parse(parts[1], CultureInfo.InvariantCulture),
                        float.Parse(parts[2], CultureInfo.InvariantCulture),
                        float.Parse(parts[3], CultureInfo.InvariantCulture)
                    ));
                    break;
                case "f":
                    // f v1/vt1/vn1 v2/vt2/vn2 v3/vt3/vn3
                    for (int i = 1; i <= 3; i++)
                    {
                        // reuse existing vertex if we've seen the same combo
                        if (!vertDict.TryGetValue(parts[i], out ushort index))
                        {
                            var comps = parts[i].Split('/');
                            int vi = int.Parse(comps[0]) - 1;
                            int ti = int.Parse(comps[1]) - 1;
                            int ni = int.Parse(comps[2]) - 1;

                            var v = new AssetVertex
                            {
                                Position = posList[vi],
                                TexCoord = uvList[ti],
                                Normal   = normList[ni],
                                VertexColor = colorList[vi],
                            };

                            index = (ushort)vertList.Count;
                            vertDict[parts[i]] = index;
                            vertList.Add(v);
                        }

                        idxList.Add(index);
                    }
                    break;
            }
        }

        vertices = vertList.ToArray();
        indices  = idxList.ToArray();
    }
}