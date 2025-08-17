using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public struct Vector3Float(float x, float y, float z)
{
    [FieldOffset(0)] public float X = x;
    [FieldOffset(4)] public float Y = y;
    [FieldOffset(8)] public float Z = z;
    
    public const int SizeInBytes = 12;
    
    public override string ToString() => $"Vector3f({X}, {Y}, {Z})";
}
