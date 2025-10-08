using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Sequential)]
public struct Box(double xMin, double yMin, double xMax, double yMax)
{
    public double Left => xMin;
    public double Right => xMax;
    public double Top => yMin;
    public double Bottom => yMax;
    public double Width => xMax - xMin;
    public double Height => yMax - yMin;
    public Vector2 Min => new(xMin, yMin);
    public Vector2 Max => new(xMax, yMax);
    public Vector2 Center => new((xMin + xMax) / 2, (yMin + yMax) / 2);
    
    public Vector2 Position = new(xMin, yMin);
    public Vector2 Size = new(xMax - xMin, yMax - yMin);
    
    public static Box Zero => new(0, 0, 0, 0);
    
    public override string ToString() => $"Box(Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom})";
}