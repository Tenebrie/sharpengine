namespace Engine.Core.Makers;

public static class QuatMakers
{
    public static Quat FromRotation(double pitch, double yaw, double roll)
    {
        return Quat.CreateFromYawPitchRoll(double.DegreesToRadians(yaw), double.DegreesToRadians(pitch), double.DegreesToRadians(roll));
    }

    public static Quat FromRotationRadians(double pitch, double yaw, double roll)
    {
        return Quat.CreateFromYawPitchRoll(yaw, pitch, roll);
    }
    
    public static Quat FromRowMatrix(in Matrix4x4 m)
    {
        double trace = m.M11 + m.M22 + m.M33;
        double qw, qx, qy, qz;

        if (trace > 0)
        {
            double s = Math.Sqrt(trace + 1.0) * 2;   // 4*qw
            qw = 0.25 * s;
            qx = (m.M32 - m.M23) / s;
            qy = (m.M13 - m.M31) / s;
            qz = (m.M21 - m.M12) / s;
        }
        else if (m.M11 > m.M22 && m.M11 > m.M33)
        {
            double s = Math.Sqrt(1.0 + m.M11 - m.M22 - m.M33) * 2;
            qw = (m.M32 - m.M23) / s;
            qx = 0.25 * s;
            qy = (m.M12 + m.M21) / s;
            qz = (m.M13 + m.M31) / s;
        }
        else if (m.M22 > m.M33)
        {
            double s = Math.Sqrt(1.0 + m.M22 - m.M11 - m.M33) * 2;
            qw = (m.M13 - m.M31) / s;
            qx = (m.M12 + m.M21) / s;
            qy = 0.25 * s;
            qz = (m.M23 + m.M32) / s;
        }
        else
        {
            double s = Math.Sqrt(1.0 + m.M33 - m.M11 - m.M22) * 2;
            qw = (m.M21 - m.M12) / s;
            qx = (m.M13 + m.M31) / s;
            qy = (m.M23 + m.M32) / s;
            qz = 0.25 * s;
        }
        return Quat.Normalize(new Quat(qx, qy, qz, qw));
    }
}