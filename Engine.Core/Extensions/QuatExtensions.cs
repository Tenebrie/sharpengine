using Engine.Core.Common;
using Engine.Core.Logging;

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
    
    public static double SignedAngleTo(this Quat a, Quat b, Vector3 axis)
    {
        // Normalize input quaternions
        var aN = Quat.Normalize(a);
        var bN = Quat.Normalize(b);

        // Relative rotation from a to b
        var delta = bN * Quat.Inverse(aN);
        delta = Quat.Normalize(delta);

        // Extract axis and angle from delta
        delta.ToAxisAngle(out var deltaAxis, out var deltaAngle);

        // Determine sign based on the reference axis
        var sign = Math.Sign(deltaAxis.DotProduct(axis.Normalized()));

        return double.RadiansToDegrees(deltaAngle) * sign;
    }
    
    public static void ToAxisAngle(this Quat q, out Vector3 axis, out double angle)
    {
        // Normalize to be safe
        var n = Quat.Normalize(q);

        // Vector part length
        var vx = n.X; var vy = n.Y; var vz = n.Z;
        var w  = n.W;

        var vLenSq = vx * vx + vy * vy + vz * vz;

        // Degenerate / identity: no reliable axis; choose any
        const double EPS = 1e-12;
        if (vLenSq < EPS)
        {
            axis = new Vector3(1, 0, 0);
            angle = 0.0;
            return;
        }

        var vLen = Math.Sqrt(vLenSq);

        // Stable angle using atan2; maps well near zero
        var a = 2.0 * Math.Atan2(vLen, w); // in (0, 2π]

        // Enforce the shortest representation: angle in [0, π]
        if (a > Math.PI)
        {
            // Flip axis and map angle to [0, π]
            a = 2.0 * Math.PI - a;
            vLen = -vLen;
        }

        axis = new Vector3(vx / vLen, vy / vLen, vz / vLen);
        angle = a; // radians
    }
}