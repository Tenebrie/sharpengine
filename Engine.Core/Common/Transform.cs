namespace Engine.Core.Common;

public class Transform
{
    private Matrix4x4 _data;

    public Vector Position
    {
        get => new(_data.M41, _data.M42, _data.M43);
        set
        {
            _data.M41 = value.X;
            _data.M42 = value.Y;
            _data.M43 = value.Z;
        }
    }
    
    public Quat Rotation
    {
        get
        {
            var s = Scale;
            var r = _data;
            r.Row1 /= s.X;
            r.Row2 /= s.Y;
            r.Row3 /= s.Z;
            return QuatHelpers.FromRowMatrix(r);
        }
        set
        {
            var s = Scale;
            var r = MatrixHelpers.FromQuaternion(Quat.Normalize(value));
            _data.M11 = r.M11 * s.X; _data.M12 = r.M12 * s.X; _data.M13 = r.M13 * s.X;
            _data.M21 = r.M21 * s.Y; _data.M22 = r.M22 * s.Y; _data.M23 = r.M23 * s.Y;
            _data.M31 = r.M31 * s.Z; _data.M32 = r.M32 * s.Z; _data.M33 = r.M33 * s.Z;
        }
    }
    
    public Vector Scale
    {
        get => new (_data.Row1.Length, _data.Row2.Length, _data.Row3.Length);
        set
        {
            _data.Row1.SetLengthIfNotZero(value.X);
            _data.Row2.SetLengthIfNotZero(value.Y);
            _data.Row3.SetLengthIfNotZero(value.Z);
        }
    }
    public Transform()
    {
        _data = Matrix4x4.Identity;
    }
    
    public Transform(Vector translation, Quat rotation, Vector scale)
    {
        var translationMatrix = MatrixHelpers.FromTranslation(translation);
        var rotationMatrix = MatrixHelpers.FromQuaternion(rotation);
        var scaleMatrix = MatrixHelpers.FromScale(scale);
        
        _data = translationMatrix * rotationMatrix * scaleMatrix;
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
        return this;
    }
    public Transform Rotate(Quat rotation)
    {
        Rotation += rotation;
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

    public override string ToString()
    {
        return $"Transform[Translation: ({_data.M14}, {_data.M24}, {_data.M34}) Rotation: ({_data.M11}, {_data.M22}, {_data.M33})Scale: ({_data.M11}, {_data.M22}, {_data.M33})";
    }

    public Matrix4x4 ToMatrix()
    {
        return _data;
    }
    
    public static Transform operator*(Transform child, Transform parent)
    {
        var result = new Transform
        {
            _data = child._data * parent._data
        };
        return result;
    }
}
