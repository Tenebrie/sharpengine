using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 8)]
public struct Vector2Float(float x, float y)
{
    [FieldOffset(0)] public float X = x;
    [FieldOffset(4)] public float Y = y;
    
    public const int SizeInBytes = 8;
    
    public override string ToString() => $"Vector2f({X}, {Y})";
}
