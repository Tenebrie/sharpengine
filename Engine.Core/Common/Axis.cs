namespace Engine.Core.Common;

public static class Axis
{
    public static Vector Up => Vector.UnitY;
    public static Vector Down => -Vector.UnitY;
    public static Vector Left => -Vector.UnitX;
    public static Vector Right => Vector.UnitX;
    public static Vector Forward => -Vector.UnitZ;
    public static Vector Backward => Vector.UnitZ;
    
    public static Vector Pitch => Vector.UnitX;
    public static Vector Yaw => Vector.UnitY;
    public static Vector Roll => Vector.UnitZ;
}