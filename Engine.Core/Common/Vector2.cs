using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct Vector2(double x, double y)
{
    [FieldOffset(00)] public double X = x;
    [FieldOffset(08)] public double Y = y;
    [FieldOffset(16)] private double _firstPadding = 0;
    [FieldOffset(24)] private double _secondPadding = 0;
    
    public double this[int i] => i switch
    {
        0 => X,
        1 => Y,
        _ => throw new IndexOutOfRangeException()
    };

    public static Vector2 Zero => Vector4.Zero;
    public static Vector2 One => Vector4.One;
    public static Vector2 UnitX => Vector4.UnitX;
    public static Vector2 UnitY => Vector4.UnitY;
    public static Vector2 UnitZ => Vector4.UnitZ;
    public static Vector2 Up => Vector4.Up;
    public static Vector2 Down => -Vector4.UnitY;
    public static Vector2 Left => -Vector4.UnitX;
    public static Vector2 Right => Vector4.UnitX;
    public static Vector2 Forward => -Vector4.UnitZ;
    public static Vector2 Backward => Vector4.UnitZ;
    public static Vector2 Pitch => Vector4.UnitX;
    public static Vector2 Yaw => Vector4.UnitY;
    public static Vector2 Roll => Vector4.UnitZ;
    
    public Vector3 ToVector3() => Unsafe.As<Vector2, Vector3>(ref this);
    public Vector4 ToVector4() => Unsafe.As<Vector2, Vector4>(ref this);
    private readonly Vector256<double> ToAccelerated() => Unsafe.As<Vector2, Vector256<double>>(ref Unsafe.AsRef(in this));
    private static Vector2 FromAccelerated(Vector256<double> vector) => Unsafe.As<Vector256<double>, Vector2>(ref vector);

    private Vector4 Promote() => ToVector4();
    public Vector2Float Downgrade()
    {
        if (!Avx.IsSupported)
            return new Vector2Float((float)X, (float)Y);
        
        var f128 = Avx.ConvertToVector128Single(ToAccelerated());
        return new Vector2Float(f128.GetElement(0), f128.GetElement(1));
    }
    
    public double Length => Math.Sqrt(LengthSquared);
    public double LengthSquared => Dot(this);
    public double DistanceTo(Vector2 other) => Math.Sqrt(Promote().DistanceSquaredTo(other));
    public double DistanceSquaredTo(Vector2 other) => Promote().DistanceSquaredTo(other);

    public double Dot(Vector2 other) => Promote().Dot(other);
    public Vector2 NormalizedCopy() => Promote().NormalizedCopy();
    public Vector2 NormalizeInPlace() => Promote().NormalizeInPlace();
    public Vector2 SetLengthIfNotZero(double length)
    {
        var currentLength = Length;
        if (currentLength < double.Epsilon)
            return this;
    
        var factor = length / currentLength;
        X *= factor;
        Y *= factor;
        return this;
    }

    public static Vector2 operator +(Vector2 a, Vector2 b) => a.Promote() + b.Promote();
    public static Vector2 operator -(Vector2 a, Vector2 b) => a.Promote() - b.Promote();
    public static Vector2 operator -(Vector2 a) => -a.Promote();
    public static Vector2 operator *(Vector2 a, double b) => a.Promote() * b;
    public static Vector2 operator *(double b, Vector2 a) => a.Promote() * b;
    public static Vector2 operator *(Vector2 a, Vector2 b) => a.Promote() * b.Promote();
    public static Vector2 operator /(Vector2 a, double b) => a.Promote() / b;
    public static Vector2 operator /(Vector2 a, Vector2 b) => a.Promote() / b.Promote();

    // Vector4
    public static implicit operator Vector2(Vector4 v) => Unsafe.As<Vector4, Vector2>(ref v);
    public static implicit operator Vector4(Vector2 v) => Unsafe.As<Vector2, Vector4>(ref v);
    // System.Numerics.Vector2
    public static implicit operator Vector2(System.Numerics.Vector2 v) => new(v.X, v.Y);
    public static implicit operator System.Numerics.Vector2(Vector2 v) => new((float)v.X, (float)v.Y);
    // Silk.NET.Maths.Vector2D<double>
    public static implicit operator Vector2(Silk.NET.Maths.Vector2D<double> v) => new(v.X, v.Y);
    public static implicit operator Silk.NET.Maths.Vector2D<double>(Vector2 v) => new(v.X, v.Y);
    
    public override string ToString() => $"Vector2({X}, {Y})";
}