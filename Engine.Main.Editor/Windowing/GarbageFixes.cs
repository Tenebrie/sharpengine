using Silk.NET.GLFW;
using Silk.NET.Maths;

namespace Engine.Hypervisor.Editor.Windowing;

public static unsafe class GarbageFixes
{
    public static Vector2D<float> GetPrimaryMonitorScale()
    {
        var api     = Glfw.GetApi();
        var monitor = api.GetPrimaryMonitor();

        float sx = 1f, sy = 1f;
        if (monitor != null)
            api.GetMonitorContentScale(monitor, out sx, out sy);

        return new Vector2D<float>(sx, sy);
    }
}
