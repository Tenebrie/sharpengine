using System.Drawing;
using Engine.Core.Common;

namespace Engine.Core.Extensions;

public static class ColorExtensions
{
    public static uint ToAbgr(this Color color)
    {
        var a = (uint)color.A & 0xFF;
        var r = (uint)color.R & 0xFF;
        var g = (uint)color.G & 0xFF;
        var b = (uint)color.B & 0xFF;

        return (a << 24) | (b << 16) | (g <<  8) | r;
    }

    public static Vector4 ToVector4(this Color color)
    {
        var r = color.R / 255f;
        var g = color.G / 255f;
        var b = color.B / 255f;
        var a = color.A / 255f;
        return new Vector4(r, g, b, a);
    }
}