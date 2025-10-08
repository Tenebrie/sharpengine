using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit, Size = 16)]
public record struct Vector4Float(float X, float Y, float Z, float W)
{
    [FieldOffset(0)]  public float X = X;
    [FieldOffset(4)]  public float Y = Y;
    [FieldOffset(8)]  public float Z = Z;
    [FieldOffset(12)] public float W = W;
    
    public byte[] ToByteArray()
    {
        var bytes = new byte[16];
        MemoryMarshal.Write(bytes, in this);
        return bytes;
    }
    
    public const int SizeInBytes = 16;

    public override string ToString() => $"Vector4f(X: {X}, Y: {Y}, Z: {Z}, W: {W})";
}
