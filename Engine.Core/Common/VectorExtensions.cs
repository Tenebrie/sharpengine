using Silk.NET.Maths;

namespace Engine.Core.Common;

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
    public static Vector NormalizedCopy(this Vector vector)
    {
        return vector.SetLengthIfNotZero(1);
    }
    public static Vector4 NormalizedCopy(this Vector4 vector)
    {
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
}