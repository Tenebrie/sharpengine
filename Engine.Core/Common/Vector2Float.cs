using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public record struct Vector2Float(float X, float Y)
{
    [FieldOffset(0)] public float X = X;
    [FieldOffset(4)] public float Y = Y;
    
    public const int SizeInBytes = 8;
    
    public override string ToString() => $"Vector2f({X}, {Y})";
}
