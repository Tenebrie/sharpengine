using System.Globalization;

namespace Engine.Core.Extensions;

public static class FloatExtensions
{
    public static string ToInvariantString(this float value, string format = "F6")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
