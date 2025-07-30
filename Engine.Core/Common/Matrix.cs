using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Engine.Core.Common;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public struct Matrix : IEquatable<Matrix>
{
    public static Matrix Identity => new
    (
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    );

    [IgnoreDataMember] public Vector4 Row1;
    [IgnoreDataMember] public Vector4 Row2;
    [IgnoreDataMember] public Vector4 Row3;
    [IgnoreDataMember] public Vector4 Row4;

    public Matrix(Vector4 row1, Vector4 row2, Vector4 row3, Vector4 row4)
    {
        Row1 = row1;
        Row2 = row2;
        Row3 = row3;
        Row4 = row4;
    }
    public Matrix(
        double m11, double m12, double m13, double m14,
        double m21, double m22, double m23, double m24,
        double m31, double m32, double m33, double m34,
        double m41, double m42, double m43, double m44)
    {
        Row1 = new Vector4(m11, m12, m13, m14);
        Row2 = new Vector4(m21, m22, m23, m24);
        Row3 = new Vector4(m31, m32, m33, m34);
        Row4 = new Vector4(m41, m42, m43, m44);
    }

    [IgnoreDataMember] public Vector4 Column1 => new(Row1.X, Row2.X, Row3.X, Row4.X);
    [IgnoreDataMember] public Vector4 Column2 => new(Row1.Y, Row2.Y, Row3.Y, Row4.Y);
    [IgnoreDataMember] public Vector4 Column3 => new(Row1.Z, Row2.Z, Row3.Z, Row4.Z);
    [IgnoreDataMember] public Vector4 Column4 => new(Row1.W, Row2.W, Row3.W, Row4.W);

    /// <summary>Value at row 1, column 1 of the matrix.</summary>
    [DataMember]
    public double M11
    {
        readonly get => Row1.X;
        set => Row1.X = value;
    }

    /// <summary>Value at row 1, column 2 of the matrix.</summary>
    [DataMember]
    public double M12
    {
        readonly get => Row1.Y;
        set => Row1.Y = value;
    }

    /// <summary>Value at row 1, column 3 of the matrix.</summary>
    [DataMember]
    public double M13
    {
        readonly get => Row1.Z;
        set => Row1.Z = value;
    }

    /// <summary>Value at row 1, column 4 of the matrix.</summary>
    [DataMember]
    public double M14
    {
        readonly get => Row1.W;
        set => Row1.W = value;
    }

    /// <summary>Value at row 2, column 1 of the matrix.</summary>
    [DataMember]
    public double M21
    {
        readonly get => Row2.X;
        set => Row2.X = value;
    }

    /// <summary>Value at row 2, column 2 of the matrix.</summary>
    [DataMember]
    public double M22
    {
        readonly get => Row2.Y;
        set => Row2.Y = value;
    }

    /// <summary>Value at row 2, column 3 of the matrix.</summary>
    [DataMember]
    public double M23
    {
        readonly get => Row2.Z;
        set => Row2.Z = value;
    }

    /// <summary>Value at row 2, column 4 of the matrix.</summary>
    [DataMember]
    public double M24
    {
        readonly get => Row2.W;
        set => Row2.W = value;
    }

    /// <summary>Value at row 3, column 1 of the matrix.</summary>
    [DataMember]
    public double M31
    {
        readonly get => Row3.X;
        set => Row3.X = value;
    }

    /// <summary>Value at row 3, column 2 of the matrix.</summary>
    [DataMember]
    public double M32
    {
        readonly get => Row3.Y;
        set => Row3.Y = value;
    }

    /// <summary>Value at row 3, column 3 of the matrix.</summary>
    [DataMember]
    public double M33
    {
        readonly get => Row3.Z;
        set => Row3.Z = value;
    }

    /// <summary>Value at row 3, column 4 of the matrix.</summary>
    [DataMember]
    public double M34
    {
        readonly get => Row3.W;
        set => Row3.W = value;
    }

    /// <summary>Value at row 4, column 1 of the matrix.</summary>
    [DataMember]
    public double M41
    {
        readonly get => Row4.X;
        set => Row4.X = value;
    }

    /// <summary>Value at row 4, column 2 of the matrix.</summary>
    [DataMember]
    public double M42
    {
        readonly get => Row4.Y;
        set => Row4.Y = value;
    }

    /// <summary>Value at row 4, column 3 of the matrix.</summary>
    [DataMember]
    public double M43
    {
        readonly get => Row4.Z;
        set => Row4.Z = value;
    }

    /// <summary>Value at row 4, column 4 of the matrix.</summary>
    [DataMember]
    public double M44
    {
        readonly get => Row4.W;
        set => Row4.W = value;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<double> ToSpan()
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix, double>(ref this), 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToFloatSpan(ref Span<float> span) => ToFloatSpan(ref span, 0);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToFloatSpan(ref Span<float> span, int offset)
    {
        ReadOnlySpan<double> src = ToSpan();

        for (var i = 0; i < 16; i++)
            span[i + offset] = (float)src[i];
    }

    public Vector4 this[int i] => i switch
    {
        0 => Row1,
        1 => Row2,
        2 => Row3,
        3 => Row4,
        _ => throw new IndexOutOfRangeException()
    };
    public double this[int row, int column] => this[row][column];
    
    public static Matrix operator *(Matrix value1, Matrix value2)
    {
        return new Matrix(
            value1.M11 * value2.Row1 + value1.M12 * value2.Row2 + value1.M13 * value2.Row3 + value1.M14 * value2.Row4,
            value1.M21 * value2.Row1 + value1.M22 * value2.Row2 + value1.M23 * value2.Row3 + value1.M24 * value2.Row4,
            value1.M31 * value2.Row1 + value1.M32 * value2.Row2 + value1.M33 * value2.Row3 + value1.M34 * value2.Row4,
            value1.M41 * value2.Row1 + value1.M42 * value2.Row2 + value1.M43 * value2.Row3 + value1.M44 * value2.Row4
        );
    }

    public bool Equals(Matrix other) => Row1.Equals(other.Row1) && Row2.Equals(other.Row2) && Row3.Equals(other.Row3) && Row4.Equals(other.Row4);
    public override bool Equals(object? obj) => obj is Matrix other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Row1, Row2, Row3, Row4);
    public static bool operator ==(Matrix left, Matrix right) => left.Equals(right);
    public static bool operator !=(Matrix left, Matrix right) => !(left == right);
}