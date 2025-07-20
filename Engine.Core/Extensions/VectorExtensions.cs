namespace Engine.Core.Extensions;

public static class VectorExtensions
{
    public static Vector Normalize(this ref Vector vector)
    {
        return vector.SetLengthIfNotZero(1);
    }
    public static Vector4 Normalize(this ref Vector4 vector)
    {
        return vector.SetLengthIfNotZero(1);
    }
    public static Vector Normalized(this Vector vector)
    {
        // Already normalized
        if (vector.LengthSquared - 1 < double.Epsilon)
            return vector;
        
        return vector.SetLengthIfNotZero(1);
    }
    public static Vector4 Normalized(this Vector4 vector)
    {
        // Already normalized
        if (vector.LengthSquared - 1 < double.Epsilon)
            return vector;
        
        return vector.SetLengthIfNotZero(1);
    }
    public static Vector SetLengthIfNotZero(this ref Vector vector, double length)
    {
        var currentLength = vector.Length;
        if (currentLength < double.Epsilon)
            return vector;
        
        var factor = length / currentLength;
        vector.X *= factor;
        vector.Y *= factor;
        vector.Z *= factor;
        return vector;
    }
    
    public static Vector4 SetLengthIfNotZero(this ref Vector4 vector, double length)
    {
        var currentLength = vector.Length;
        if (currentLength < double.Epsilon)
            return vector;
        
        var factor = length / currentLength;
        vector.X *= factor;
        vector.Y *= factor;
        vector.Z *= factor;
        vector.W *= factor;
        return vector;
    }
    
    public static double Dot(this Vector a, Vector b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    public static double Dot(this Vector4 a, Vector4 b)
    {
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
    }

    public static Vector Cross(this Vector a, Vector b)
    {
        return new Vector(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X
        );
    }
}