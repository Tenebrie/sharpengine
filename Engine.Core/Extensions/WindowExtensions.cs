using Engine.Core.Logging;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Core.Extensions;

public static class WindowExtensions
{
    public static float GetBaseResolutionScale(this IWindow window)
    {
        return (float)window.Size.X / window.FramebufferSize.X;
    }
    
    public static float GetResolutionScale(this IWindow window)
    {
        return GetBaseResolutionScale(window) * 1.0f;
    }

    public static Vector2D<int> GetScaledFramebufferSize(this IWindow window)
    {
        return new Vector2D<int>(
            (int)Math.Round(window.FramebufferSize.X * window.GetResolutionScale()),
            (int)Math.Round(window.FramebufferSize.Y * window.GetResolutionScale())
        );
    }
}