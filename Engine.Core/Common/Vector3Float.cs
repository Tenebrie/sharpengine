using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 12)]
public record struct Vector3Float(float X, float Y, float Z)
{
    [FieldOffset(0)] public float X = X;
    [FieldOffset(4)] public float Y = Y;
    [FieldOffset(8)] public float Z = Z;
    
    public const int SizeInBytes = 12;
    
    public override string ToString() => $"Vector3f({X}, {Y}, {Z})";
}
