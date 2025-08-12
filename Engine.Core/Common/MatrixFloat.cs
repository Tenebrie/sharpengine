using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 64)]
public struct MatrixFloat(Vector4Float row1, Vector4Float row2, Vector4Float row3, Vector4Float row4)
{
    [FieldOffset(00)] public Vector4Float Row1 = row1;
    [FieldOffset(16)] public Vector4Float Row2 = row2;
    [FieldOffset(32)] public Vector4Float Row3 = row3;
    [FieldOffset(48)] public Vector4Float Row4 = row4;
    
    public const int SizeInBytes = 64;
}