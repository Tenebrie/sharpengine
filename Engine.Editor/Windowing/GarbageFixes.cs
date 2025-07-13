using System.Runtime.InteropServices;
using Silk.NET.GLFW;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Editor.Windowing;

public static unsafe partial class GarbageFixes
{
    [LibraryImport("glfw3", EntryPoint = "glfwGetWindowContentScale")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial void GlfwGetWindowContentScale(
        WindowHandle* window, out float scaleX, out float scaleY);

    public static Vector2D<float> GetWindowContentScale(IWindow window)
    {
        if (window.Native == null)
        {
            return new Vector2D<float>(1.0f, 1.0f);
        }

        var win = window.Native?.Glfw;
        if (win == null)
        {
            return new Vector2D<float>(1.0f, 1.0f);
        }
        GlfwGetWindowContentScale((WindowHandle*)win, out var sx, out var sy);
        return new Vector2D<float>(sx, sy);
    }
    
    public static Vector2D<float> GetPrimaryMonitorScale()
    {
        var api     = Glfw.GetApi();          // Silk has already initialised GLFW for you
        var monitor = api.GetPrimaryMonitor();   // never null unless GLFW failed to init

        float sx = 1f, sy = 1f;               // sensible fallback = 100 %
        if (monitor != null)
            api.GetMonitorContentScale(monitor, out sx, out sy);

        return new Vector2D<float>(sx, sy);   // e.g. 1.00, 1.25, 2.00 …
    }
}
