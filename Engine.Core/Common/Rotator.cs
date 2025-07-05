namespace Engine.Core.Common;

public class Rotator(double pitch, double yaw, double roll)
{
    public double Pitch { get; set; } = pitch;
    public double Yaw { get; set; } = yaw;
    public double Roll { get; set; } = roll;

    public static Rotator Identity => new Rotator(0, 0, 0);
    
    public Quat ToQuat()
    {
        return Quat.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
    }
}