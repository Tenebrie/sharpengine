using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Engine.Core.Extensions;

namespace Engine.Core.Common;

public record struct Vector4Shorthand(double X, double Y, double Z, double W)
{
    public static implicit operator Vector4Shorthand(double x) => new(x, x, x, x);
    public static implicit operator Vector4Shorthand((double x, double y) t) => new(t.x, t.x, t.y, t.y);
    public static implicit operator Vector4Shorthand((double x, double y, double z, double w) t) => new(t.x, t.y, t.z, t.w);
    
    public static implicit operator Vector4(Vector4Shorthand t) => new(t.X, t.Y, t.Z, t.W);
}
