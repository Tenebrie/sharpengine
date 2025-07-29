using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
public struct Vector2Float(float x, float y)
{
    [FieldOffset(0)] public float X = x;
    [FieldOffset(4)] public float Y = y;
}
