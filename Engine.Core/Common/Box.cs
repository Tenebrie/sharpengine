using System.Runtime.InteropServices;

namespace Engine.Core.Common;

[StructLayout(LayoutKind.Sequential)]
public record struct Box(double Left, double Top, double Right, double Bottom)
{
    public double Width => Right - Left;
    public double Height => Bottom - Top;
    public Vector2 Min => new(Left, Top);
    public Vector2 Max => new(Right, Bottom);
    public Vector2 Center => new((Left + Right) / 2, (Top + Bottom) / 2);
    
    public Vector2 Position = new(Left, Top);
    public Vector2 Size = new(Right - Left, Bottom - Top);
    
    public static Box Zero => new(0, 0, 0, 0);
    public static Box Full => new(0, 0, 1, 1);
    public static Box FillFancy(double progress) => new(0, 0, progress, progress);
    public static Box FillTop(double progress) => new(0, 1 - progress, 1, 1);
    public static Box FillBottom(double progress) => new(0, 0, 1, progress);
    public static Box FillLeft(double progress) => new(1 - progress, 0, 1, 1);
    public static Box FillRight(double progress) => new(0, 0, progress, 1);
    
    public override string ToString() => $"Box(Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom})";
}