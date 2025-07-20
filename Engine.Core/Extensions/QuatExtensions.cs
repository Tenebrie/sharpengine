namespace Engine.Core.Extensions;

public static class QuatExtensions
{
    public static double AngleTo(this Quat a, Quat b)
    {
        // Normalize quaternions to ensure they represent valid rotations
        var aN = Quat.Normalize(a);
        var bN = Quat.Normalize(b);
    
        // Calculate dot product between quaternions
        var dot = aN.X * bN.X + aN.Y * bN.Y + aN.Z * bN.Z + aN.W * bN.W;
    
        // Handle the case where quaternions represent the same rotation in opposite forms
        dot = Math.Abs(dot);
    
        // Clamp to valid range to handle floating point errors
        dot = Math.Clamp(dot, -1.0, 1.0);
    
        // Calculate angle in radians (2 * acos(dot))
        var angleRadians = 2.0 * Math.Acos(dot);
    
        // Convert to degrees
        return double.RadiansToDegrees(angleRadians);
    }
}