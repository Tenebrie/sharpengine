using System.Runtime.CompilerServices;
using Engine.Core.Extensions;
using Engine.Core.Makers;

namespace Engine.Core.Common;

public class Transform
{
    // Row major, translation in last row
    protected Matrix Data;

    public virtual Vector3 Position
    {
        get => new(Data.M41, Data.M42, Data.M43);
        set
        {
            Data.M41 = value.X;
            Data.M42 = value.Y;
            Data.M43 = value.Z;
        }
    }
    
    public virtual Quat Rotation
    {
        get
        {
            var s = Scale;
            var r = Data;
            r.Row1 /= s.X;
            r.Row2 /= s.Y;
            r.Row3 /= s.Z;
            return QuatMakers.FromRowMatrix(r);
        }
        set
        {
            var s = Scale;
            var r = MatrixMakers.FromQuaternion(Quat.Normalize(value));
            Data.M11 = r.M11 * s.X; Data.M12 = r.M12 * s.X; Data.M13 = r.M13 * s.X;
            Data.M21 = r.M21 * s.Y; Data.M22 = r.M22 * s.Y; Data.M23 = r.M23 * s.Y;
            Data.M31 = r.M31 * s.Z; Data.M32 = r.M32 * s.Z; Data.M33 = r.M33 * s.Z;
        }
    }
    
    public virtual Vector3 Scale
    {
        get => new (Data.Row1.Length, Data.Row2.Length, Data.Row3.Length);
        set
        {
            Data.Row1.SetLengthIfNotZero(value.X);
            Data.Row2.SetLengthIfNotZero(value.Y);
            Data.Row3.SetLengthIfNotZero(value.Z);
        }
    }
    
    public Basis Basis
    {
        get => new()
        {
            XAxis = new Vector3(Data.M11, Data.M12, Data.M13),
            YAxis = new Vector3(Data.M21, Data.M22, Data.M23),
            ZAxis = new Vector3(Data.M31, Data.M32, Data.M33)
        };
        set
        {
            var scale = Scale;
            Data.M11 = value.XAxis.X * scale.X; Data.M12 = value.XAxis.Y * scale.X; Data.M13 = value.XAxis.Z * scale.X;
            Data.M21 = value.YAxis.X * scale.Y; Data.M22 = value.YAxis.Y * scale.Y; Data.M23 = value.YAxis.Z * scale.Y;
            Data.M31 = value.ZAxis.X * scale.Z; Data.M32 = value.ZAxis.Y * scale.Z; Data.M33 = value.ZAxis.Z * scale.Z;
        }
    }

    protected Transform()
    {
        Data = Matrix.Identity;
    }

    private Transform(Vector3 translation, Quat rotation, Vector3 scale)
    {
        var translationMatrix = MatrixMakers.FromTranslation(translation);
        var rotationMatrix = MatrixMakers.FromQuaternion(rotation);
        var scaleMatrix = MatrixMakers.FromScale(scale);
        
        Data = translationMatrix * rotationMatrix * scaleMatrix;
    }
    
    public Transform TranslateLocal(double x, double y, double z)
    {
        Position += Basis.TransformVector(new Vector3(x, y, z));
        return this;
    }
    public Transform TranslateLocal(Vector3 translation)
    {
        Position += Basis.TransformVector(translation);
        return this;
    }
    public Transform TranslateGlobal(double x, double y, double z)
    {
        Position += new Vector3(x, y, z);
        return this;
    }
    public Transform TranslateGlobal(Vector3 translation)
    {
        Position += translation;
        return this;
    }

    public Transform Rotate(double pitch, double yaw, double roll)
    {
        Rotation *= QuatMakers.FromRotation(pitch, yaw, roll);
        return this;
    }
    public Transform Rotate(Quat rotation)
    {
        Rotation *= rotation;
        return this;
    }
    
    public Transform RotateAroundGlobal(Vector3 axis, double angle)
    {
        var rotation = Quat.CreateFromAxisAngle(axis, double.DegreesToRadians(angle));
        Rotation *= rotation;
        return this;
    }
    
    public Transform RotateAroundLocal(Vector3 axis, double angle)
    {
        var worldAxis = Basis.TransformVector(axis.NormalizedCopy());
        var rotation = Quat.CreateFromAxisAngle(worldAxis, double.DegreesToRadians(angle));
    
        // Apply the rotation
        Rotation *= rotation;
        return this;
    }

    public Transform Rescale(double scale)
    {
        Scale *= new Vector3(scale, scale, scale);
        return this;
    }
    public Transform Rescale(double x, double y, double z)
    {
        Scale *= new Vector3(x, y, z);
        return this;
    }
    public Transform Rescale(Vector3 scale)
    {
        Scale *= scale;
        return this;
    }

    public Transform Inverse()
    {
        var newTransform = new Transform();
        Inverse(ref newTransform);
        return newTransform;
    }
    
    public void Inverse(ref Transform inverse)
    {
        var m = Data;
    
        // Calculate the determinants for the 3x3 submatrices
        var det1 = m.M22 * m.M33 - m.M23 * m.M32;
        var det2 = m.M21 * m.M33 - m.M23 * m.M31;
        var det3 = m.M21 * m.M32 - m.M22 * m.M31;
        var det4 = m.M12 * m.M33 - m.M13 * m.M32;
        var det5 = m.M11 * m.M33 - m.M13 * m.M31;
        var det6 = m.M11 * m.M32 - m.M12 * m.M31;
        var det7 = m.M12 * m.M23 - m.M13 * m.M22;
        var det8 = m.M11 * m.M23 - m.M13 * m.M21;
        var det9 = m.M11 * m.M22 - m.M12 * m.M21;
    
        // Calculate the determinant of the 3x3 matrix
        var determinant = m.M11 * det1 - m.M12 * det2 + m.M13 * det3;
    
        if (Math.Abs(determinant) < double.Epsilon)
        {
            // Matrix is singular, can't invert
            inverse = Identity;
            return;
        }
    
        var invDet = 1.0f / determinant;
    
        // Calculate the adjugate matrix and multiply by 1/determinant
        var inv = new Matrix(
            invDet * det1, -invDet * det4, invDet * det7, 0f,
            -invDet * det2, invDet * det5, -invDet * det8, 0f,
            invDet * det3, -invDet * det6, invDet * det9, 0f,
            0f, 0f, 0f, 1f
        );
    
        // Calculate the inverse translation
        var tx = m.M41;
        var ty = m.M42;
        var tz = m.M43;
    
        var invX = -(tx * inv.M11 + ty * inv.M21 + tz * inv.M31);
        var invY = -(tx * inv.M12 + ty * inv.M22 + tz * inv.M32);
        var invZ = -(tx * inv.M13 + ty * inv.M23 + tz * inv.M33);
    
        inv.M41 = invX;
        inv.M42 = invY;
        inv.M43 = invZ;
    
        inverse.Data = inv;
    }
    
    // Camera doesn't need scale, so we can skip it
    public void InverseWithoutScale(ref Transform inverse)
    {
        var m = Data;

        var inv = new Matrix(
            m.M11, m.M21, m.M31, 0f,
            m.M12, m.M22, m.M32, 0f,
            m.M13, m.M23, m.M33, 0f,
            0f,    0f,    0f,    1f
        );

        var tx = m.M41;
        var ty = m.M42;
        var tz = m.M43;

        var invX = -(tx * inv.M11 + ty * inv.M21 + tz * inv.M31);
        var invY = -(tx * inv.M12 + ty * inv.M22 + tz * inv.M32);
        var invZ = -(tx * inv.M13 + ty * inv.M23 + tz * inv.M33);

        inv.M41 = invX;
        inv.M42 = invY;
        inv.M43 = invZ;

        inverse.Data = inv;
    }
    
    public override string ToString()
    {
        var rotation = Rotation;
        var scale = Scale;
        return $"Transform [" +
               $"Translation: ({Data.M41}, {Data.M42}, {Data.M43}), " +
               $"Rotation: ({rotation.X}, {rotation.Y}, {rotation.Z}, {rotation.W}), " +
               $"Scale: ({scale.X}, {scale.Y}, {scale.Z})" +
               $"]";
    }

    public Matrix ToMatrix()
    {
        return Data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToFloatSpan(ref Span<float> span) => Data.ToFloatSpan(ref span);
    
    public static Transform Identity => new();
    public static Transform Copy(Transform instance) => new() { Data = instance.Data };
    
    public static Transform FromTranslation(Vector3 translation) => new (translation, Quat.Identity, Vector3.One);
    public static Transform FromTranslation(double x, double y, double z) => new (new Vector3(x, y, z), Quat.Identity, Vector3.One);
    public static Transform FromRotation(Quat rotation) => new (Vector3.Zero, rotation, Vector3.One);

    public static Transform FromRotation(double pitch, double yaw, double roll)
    {
        return new Transform(Vector3.Zero, QuatMakers.FromRotation(pitch, yaw, roll), Vector3.One);
    }
    public static Transform FromRotationRadians(double pitch, double yaw, double roll) => new (Vector3.Zero, QuatMakers.FromRotationRadians(pitch, yaw,  roll), Vector3.One);
    public static Transform FromScale(Vector3 scale) => new (Vector3.Zero, Quat.Identity, scale);
    public static Transform FromScale(double x, double y, double z) => new (Vector3.Zero, Quat.Identity, new Vector3(x, y, z));
    
    public static Transform operator*(Transform child, Transform parent)
    {
        var result = new Transform
        {
            Data = child.Data * parent.Data
        };
        return result;
    }
    
    public void Multiply(in Transform child, ref Transform result)
    {
        result.Data = child.Data * Data;
    }
    public void Multiply(in Matrix child, ref Matrix result)
    {
        result = child * Data;
    }
}
