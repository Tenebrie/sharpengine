using System.Drawing;

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
}