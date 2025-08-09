using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
public struct Vector4Float(float x, float y, float z, float w)
{
    [FieldOffset(0)]  public float X = x;
    [FieldOffset(4)]  public float Y = y;
    [FieldOffset(8)]  public float Z = z;
    [FieldOffset(12)] public float W = w;
    
    public byte[] ToByteArray()
    {
        var bytes = new byte[16];
        MemoryMarshal.Write(bytes, in this);
        return bytes;
    }
}
