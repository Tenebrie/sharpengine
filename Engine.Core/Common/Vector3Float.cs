using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
public struct Vector3Float(float x, float y, float z)
{
    [FieldOffset(0)] public float X = x;
    [FieldOffset(4)] public float Y = y;
    [FieldOffset(8)] public float Z = z;
}
