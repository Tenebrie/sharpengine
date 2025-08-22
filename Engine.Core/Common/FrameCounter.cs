using System.Diagnostics.CodeAnalysis;

namespace Engine.Core.Common;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class FrameCounter
{
    public static int Current { get; private set; } = 0;

    public static void Increment()
    {
        Current++;
    }
}