using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Maths;

namespace Engine.Core.Common;

public class Transform
{
    // Row major, translation in last row
    protected Matrix4x4 Data;

    public virtual Vector Position
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
            return QuatHelpers.FromRowMatrix(r);
        }
        set
        {
            var s = Scale;
            var r = MatrixHelpers.FromQuaternion(Quat.Normalize(value));
            Data.M11 = r.M11 * s.X; Data.M12 = r.M12 * s.X; Data.M13 = r.M13 * s.X;
            Data.M21 = r.M21 * s.Y; Data.M22 = r.M22 * s.Y; Data.M23 = r.M23 * s.Y;
            Data.M31 = r.M31 * s.Z; Data.M32 = r.M32 * s.Z; Data.M33 = r.M33 * s.Z;
        }
    }
    
    public virtual Vector Scale
    {
        get => new (Data.Row1.Length, Data.Row2.Length, Data.Row3.Length);
        set
        {
            Data.Row1.SetLengthIfNotZero(value.X);
            Data.Row2.SetLengthIfNotZero(value.Y);
            Data.Row3.SetLengthIfNotZero(value.Z);
        }
    }

    protected Transform()
    {
        Data = Matrix4x4.Identity;
    }

    private Transform(Vector translation, Quat rotation, Vector scale)
    {
        var translationMatrix = MatrixHelpers.FromTranslation(translation);
        var rotationMatrix = MatrixHelpers.FromQuaternion(rotation);
        var scaleMatrix = MatrixHelpers.FromScale(scale);
        
        Data = translationMatrix * rotationMatrix * scaleMatrix;
    }
    
    public Transform Translate(double x, double y, double z)
    {
        Position += new Vector(x, y, z);
        return this;
    }
    public Transform Translate(Vector translation)
    {
        Position += translation;
        return this;
    }

    public Transform Rotate(double pitch, double yaw, double roll)
    {
        Rotation *= QuatUtils.FromRotation(pitch, yaw, roll);
        // Console.WriteLine(Position);
        // Console.WriteLine(Data);
        // Rotation *= QuatUtils.FromRotation(pitch, yaw, roll);
        // RotateAround(Position, QuatUtils.FromRotation(pitch, yaw, roll));
        // var translation = Vector;
        // var ppp = Position;
        // var savedPosition = MatrixHelpers.FromTranslation(Position);
        // Position = new Vector(0, 0, 0);
        // Rotation = QuatUtils.FromRotation(pitch, yaw, roll) * Rotation;
        // Position = new Vector(10, 0, 0);
        // var r = MatrixHelpers.FromQuaternion(QuatUtils.FromRotation(pitch, yaw, roll) * Rotation);
        // var t = MatrixHelpers.FromTranslation(Position);
        // Transpose T
        // var newT = new Matrix4X4<double>(
        //     t.M11, t.M21, t.M31, t.M41,
        //     t.M12, t.M22, t.M32, t.M42,
        //     t.M13, t.M23, t.M33, t.M43,
        //     t.M14, t.M24, t.M34, t.M44
        // );
        
        // var s = MatrixHelpers.FromScale(Scale);
        // var r = MatrixHelpers.FromQuaternion(Rotation * QuatUtils.FromRotation(0, 0, 0.01));
        // Console.WriteLine(QuatUtils.FromRotation(0, 0, 0.01));
        // Data = newT * r;
        
        // Position = ppp;
        // Console.WriteLine(Data);
        return this;
    }
    public Transform Rotate(Quat rotation)
    {
        Rotation *= rotation;
        return this;
    }
    
    public Transform RotateAround(Vector point, Quat rotation)
    {
        // 1. Create transformation to move pivot to origin
        var toOrigin = MatrixHelpers.FromTranslation(point);
    
        // 2. Apply rotation
        var rotMat = MatrixHelpers.FromQuaternion(rotation);
    
        // 3. Move back to original position
        var fromOrigin = MatrixHelpers.FromTranslation(-point);
    
        // Compose: T_back * R * T_origin
        var rotationAroundPoint = fromOrigin * rotMat * toOrigin;
    
        // Apply to current transform
        Data = rotationAroundPoint * Data;
        return this;
    }

    public Transform Rescale(double x, double y, double z)
    {
        Scale *= new Vector(x, y, z);
        return this;
    }
    public Transform Rescale(Vector scale)
    {
        Scale *= scale;
        return this;
    }

    public Transform Negate()
    {
        var m = Data;

        var inv = new Matrix4x4(
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

        return new Transform { Data = inv };
    }
    
    public override string ToString()
    {
        return $"Transform[Translation: ({Data.M14}, {Data.M24}, {Data.M34}) Rotation: ({Data.M11}, {Data.M22}, {Data.M33})Scale: ({Data.M11}, {Data.M22}, {Data.M33})";
    }

    public Matrix4x4 ToMatrix()
    {
        return Data;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<double> ToSpan()
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<Matrix4x4, double>(ref Data), 16);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ToFloatSpan(ref Span<float> span)
    {
        ReadOnlySpan<double> src = ToSpan();

        for (var i = 0; i < 16; i++)
            span[i] = (float)src[i];      // narrowing cast
    }
    
    public static Transform Identity => new();
    
    public static Transform FromTranslation(Vector translation) => new (translation, Quat.Identity, Vector.One);
    public static Transform FromTranslation(double x, double y, double z) => new (new Vector(x, y, z), Quat.Identity, Vector.One);
    public static Transform FromRotation(Quat rotation) => new (Vector.Zero, rotation, Vector.One);

    public static Transform FromRotation(double pitch, double yaw, double roll)
    {
        return new Transform(Vector.Zero, QuatUtils.FromRotation(pitch, yaw, roll), Vector.One);
    }
    public static Transform FromRotationRadians(double pitch, double yaw, double roll) => new (Vector.Zero, QuatUtils.FromRotationRadians(pitch, yaw,  roll), Vector.One);
    public static Transform FromScale(Vector scale) => new (Vector.Zero, Quat.Identity, scale);
    public static Transform FromScale(double x, double y, double z) => new (Vector.Zero, Quat.Identity, new Vector(x, y, z));
    
    public static Transform operator*(Transform child, Transform parent)
    {
        var result = new Transform
        {
            Data = child.Data * parent.Data
        };
        return result;
    }
    
    public static void Multiply(in Transform child, in Transform parent, ref Transform result)
    {
        result.Data = child.Data * parent.Data;
    }
}
