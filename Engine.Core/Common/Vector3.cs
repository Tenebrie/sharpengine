using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public struct Vector3(double x, double y, double z)
{
    [FieldOffset(00)] public double X = x;
    [FieldOffset(08)] public double Y = y;
    [FieldOffset(16)] public double Z = z;
    [FieldOffset(24)] private double _padding = 0;
    
    public double this[int i] => i switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        _ => throw new IndexOutOfRangeException()
    };
    
    public static Vector3 Zero => Vector4.Zero;
    public static Vector3 One => Vector4.One;
    public static Vector3 UnitX => Vector4.UnitX;
    public static Vector3 UnitY => Vector4.UnitY;
    public static Vector3 UnitZ => Vector4.UnitZ;
    public static Vector3 Up => Vector4.Up;
    public static Vector3 Down => -Vector4.UnitY;
    public static Vector3 Left => -Vector4.UnitX;
    public static Vector3 Right => Vector4.UnitX;
    public static Vector3 Forward => -Vector4.UnitZ;
    public static Vector3 Backward => Vector4.UnitZ;
    public static Vector3 Pitch => Vector4.UnitX;
    public static Vector3 Yaw => Vector4.UnitY;
    public static Vector3 Roll => Vector4.UnitZ;
    
    public Vector2 ToVector2() => Unsafe.As<Vector3, Vector2>(ref this);
    public Vector4 ToVector4() => Unsafe.As<Vector3, Vector4>(ref this);

    private readonly Vector4 Promote() => Unsafe.As<Vector3, Vector4>(ref Unsafe.AsRef(in this));
    
    public double Length => Math.Sqrt(LengthSquared);
    public double LengthSquared => Dot(this);
    public readonly double DistanceTo(Vector3 other) => Math.Sqrt(Promote().DistanceSquaredTo(other));
    public readonly double DistanceSquaredTo(Vector3 other) => Promote().Dot(other);

    public readonly double Dot(Vector3 other) => Promote().Dot(other);
    public Vector3 Cross(Vector3 b) => new(
        Y * b.Z - Z * b.Y,
        Z * b.X - X * b.Z,
        X * b.Y - Y * b.X
    );
    
    public Vector3 NormalizedCopy() => Promote().NormalizedCopy();
    public Vector3 NormalizeInPlace() => Promote().NormalizeInPlace();

    public Vector3 SetLengthIfNotZero(double length)
    {
        var currentLength = Length;
        if (currentLength < double.Epsilon)
            return this;
    
        var factor = length / currentLength;
        X *= factor;
        Y *= factor;
        Z *= factor;
        return this;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b) => a.Promote() + b.Promote();
    public static Vector3 operator -(Vector3 a, Vector3 b) => a.Promote() - b.Promote();
    public static Vector3 operator -(Vector3 a) => -a.Promote();
    public static Vector3 operator *(Vector3 a, double b) => a.Promote() * b;
    public static Vector3 operator *(double b, Vector3 a) => a.Promote() * b;
    public static Vector3 operator *(Vector3 a, Vector3 b) => a.Promote() * b.Promote();
    public static Vector3 operator /(Vector3 a, double b) => a.Promote() / b;
    public static Vector3 operator /(Vector3 a, Vector3 b) => a.Promote() / b.Promote();
    
    // Vector4
    public static implicit operator Vector3(Vector4 v) => Unsafe.As<Vector4, Vector3>(ref v);
    public static implicit operator Vector4(Vector3 v) => Unsafe.As<Vector3, Vector4>(ref v);
    // System.Numerics.Vector2
    public static implicit operator Vector3(System.Numerics.Vector3 v) => new(v.X, v.Y, v.Z);
    public static implicit operator System.Numerics.Vector3(Vector3 v) => new((float)v.X, (float)v.Y, (float)v.Z);
    // Silk.NET.Maths.Vector2D<double>
    public static implicit operator Vector3(Silk.NET.Maths.Vector3D<double> v) => new(v.X, v.Y, v.Z);
    public static implicit operator Silk.NET.Maths.Vector3D<double>(Vector3 v) => new(v.X, v.Y, v.Z);
    
    public override string ToString() => $"Vector3({X}, {Y}, {Z})";
}
