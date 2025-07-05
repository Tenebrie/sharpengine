namespace Engine.Core.Common;

public static class QuatUtils
{
    public static Quat FromRotation(double pitch, double yaw, double roll)
    {
        // TODO: Remap -Z forward +Y Up to 
        return Quat.CreateFromYawPitchRoll(double.DegreesToRadians(yaw), double.DegreesToRadians(pitch), double.DegreesToRadians(roll));
    }

    public static Quat FromRotationRadians(double pitch, double yaw, double roll)
    {
        return Quat.CreateFromYawPitchRoll(yaw, pitch, roll);
    }
}