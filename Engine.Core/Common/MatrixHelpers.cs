using System.Runtime.CompilerServices;

namespace Engine.Core.Common;

public static class MatrixHelpers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromTranslation(Vector t) => FromTranslation(t.X, t.Y, t.Z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromTranslation(double x, double y, double z) => new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        x, y, z, 1);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromQuaternion(Quat q)
    {
        var (x, y, z, w) = (q.X, q.Y, q.Z, q.W);
        var xx = x * x; var yy = y * y; var zz = z * z;
        var xy = x * y; var xz = x * z; var yz = y * z;
        var wx = w * x; var wy = w * y; var wz = w * z;

        return new Matrix4x4(
            1 - 2 * (yy + zz), 2 * (xy - wz),     2 * (xz + wy),     0,
            2 * (xy + wz),     1 - 2 * (xx + zz), 2 * (yz - wx),     0,
            2 * (xz - wy),     2 * (yz + wx),     1 - 2 * (xx + yy), 0,
            0,                 0,                 0,                 1);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromScale(Vector s) => FromScale(s.X, s.Y, s.Z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromScale(double x, double y, double z) => new(
        x, 0, 0, 0,
        0, y, 0, 0,
        0, 0, z, 0,
        0, 0, 0, 1);
}
