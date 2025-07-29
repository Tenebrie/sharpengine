using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Explicit)]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct Vector4(double x, double y, double z, double w) : IEquatable<Vector4>
{
    [FieldOffset(00)] public double X = x;
    [FieldOffset(08)] public double Y = y;
    [FieldOffset(16)] public double Z = z;
    [FieldOffset(24)] public double W = w;
    
    /** Indexers */
    
    public double this[int i] => i switch
    {
        0 => X,
        1 => Y,
        2 => Z,
        3 => W,
        _ => throw new IndexOutOfRangeException()
    };
    
    /**
     * Constructors
     */
    
    public Vector4(Vector4 other): this(other.X, other.Y, other.Z, other.W) {}
    
    /**
     * Constant constructors
     */
    
    public static Vector4 Zero => new(0.0, 0.0, 0.0, 0.0);
    public static Vector4 One => new(1.0, 1.0, 1.0, 1.0);
    public static Vector4 UnitX => new(1.0, 0.0, 0.0, 0.0);
    public static Vector4 UnitY => new(0.0, 1.0, 0.0, 0.0);
    public static Vector4 UnitZ => new(0.0, 0.0, 1.0, 0.0);
    public static Vector4 Up => UnitY;
    public static Vector4 Down => -UnitY;
    public static Vector4 Left => -UnitX;
    public static Vector4 Right => UnitX;
    public static Vector4 Forward => -UnitZ;
    public static Vector4 Backward => UnitZ;
    public static Vector4 Pitch => UnitX;
    public static Vector4 Yaw => UnitY;
    public static Vector4 Roll => UnitZ;

    /**
     * Conversion methods
     */
    
    public Vector2 ToVector2() => Unsafe.As<Vector4, Vector2>(ref this);
    public Vector3 ToVector3() => Unsafe.As<Vector4, Vector3>(ref this);
    private readonly Vector256<double> ToAccelerated() => Unsafe.As<Vector4, Vector256<double>>(ref Unsafe.AsRef(in this));
    public static Vector4 FromVector2(Vector2 vector) => Unsafe.As<Vector2, Vector4>(ref vector);
    public static Vector4 FromVector3(Vector3 vector) => Unsafe.As<Vector3, Vector4>(ref vector);
    private static Vector4 FromAccelerated(Vector256<double> vector) => Unsafe.As<Vector256<double>, Vector4>(ref vector);

    /**
     * Common methods
     */
    
    public readonly double Length => Math.Sqrt(LengthSquared);
    public readonly double LengthSquared => Dot(this);
    public readonly double DistanceTo(Vector4 other) => Math.Sqrt(DistanceSquaredTo(other));
    public readonly double DistanceSquaredTo(Vector4 other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        var dz = Z - other.Z;
        var dw = W - other.W;
        return dx*dx + dy*dy + dz*dz + dw*dw;
    }

    public readonly double Dot(Vector4 other)
    {
        if (!(Avx.IsSupported && Sse3.IsSupported))
            return X * other.X + Y * other.Y + Z * other.Z + W * other.W;
        
        var a = ToAccelerated();
        var b = other.ToAccelerated();
        var prod = Avx.Multiply(a, b);
        var topHalf = Avx.Permute2x128(prod, prod, 0x01);
        var pairSum = Avx.Add(prod, topHalf);
        var lo = pairSum.GetLower();
        var horizontalSum = Sse3.HorizontalAdd(lo, lo);
        return horizontalSum.ToScalar();
    }

    public Vector4 NormalizedCopy() => new Vector4(this).SetLengthIfNotZero(1);

    public Vector4 NormalizeInPlace()
    {
        this = SetLengthIfNotZero(1);
        return this;
    }

    /**
     * Uncommon methods
     */
    
    public Vector4 SetLengthIfNotZero(double length)
    {
        var currentLength = Length;
        if (currentLength < double.Epsilon)
            return this;
        
        var factor = length / currentLength;
        X *= factor;
        Y *= factor;
        Z *= factor;
        W *= factor;
        return this;
    }
    
    /**
     * Operator overloads
     */

    // Addition
    public static Vector4 operator +(Vector4 a, Vector4 b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        
        return FromAccelerated(Avx.Add(a.ToAccelerated(), b.ToAccelerated()));
    }
    
    // Substraction
    public static Vector4 operator -(Vector4 a, Vector4 b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        
        return FromAccelerated(Avx.Subtract(a.ToAccelerated(), b.ToAccelerated()));
    }
    public static Vector4 operator -(Vector4 a) => new(-a.X, -a.Y, -a.Z, -a.W);
    
    // Multiplication
    public static Vector4 operator *(Vector4 a, double b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X * b, a.Y * b, a.Z * b, a.W * b);
        
        return FromAccelerated(Avx.Multiply(a.ToAccelerated(), Vector256.Create(b)));
    }
    public static Vector4 operator *(double b, Vector4 a) => a * b;
    public static Vector4 operator *(Vector4 a, Vector4 b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        
        return FromAccelerated(Avx.Multiply(a.ToAccelerated(), b.ToAccelerated()));
    }
    
    // Division
    public static Vector4 operator /(Vector4 a, double b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X / b, a.Y / b, a.Z / b, a.W / b);
        
        return FromAccelerated(Avx.Divide(a.ToAccelerated(), Vector256.Create(b)));
    }
    public static Vector4 operator /(double b, Vector4 a)
    {
        if (!Avx.IsSupported)
            return new Vector4(b / a.X, b / a.Y, b / a.Z, b / a.W);
        
        return FromAccelerated(Avx.Divide(Vector256.Create(b), a.ToAccelerated()));
    }
    public static Vector4 operator /(Vector4 a, Vector4 b)
    {
        if (!Avx.IsSupported)
            return new Vector4(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
        
        return FromAccelerated(Avx.Divide(a.ToAccelerated(), b.ToAccelerated()));
    }
    
    /**
     * Equality check
     */
    
    public bool Equals(Vector4 other)
    {
        if (!Avx.IsSupported)
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        var compareResult = Avx.CompareEqual(ToAccelerated(), other.ToAccelerated());
        return Avx.MoveMask(compareResult) == 0b1111;
    }

    public override bool Equals(object? obj) => obj is Vector4 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);
    public static bool operator !=(Vector4 left, Vector4 right) => !(left == right);
    
    /**
     * Implicit conversions
     */
    
    public static implicit operator Silk.NET.Maths.Vector4D<double>(Vector4 v) => new(v.X, v.Y, v.Z, v.W);
    public static implicit operator System.Numerics.Vector4(Vector4 v) => new((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
    
    /**
     * Others
     */
    
    public override string ToString() => $"Vector4({X}, {Y}, {Z}, {W})";
}