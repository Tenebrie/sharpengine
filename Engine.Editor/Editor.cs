using Engine.Rendering.Bgfx;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Editor;

internal class Editor
{
    private static void Main()
    {
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion()),
        };
        var window = Window.Create(opts);
        window.Render += BgfxCore.Frame;
        window.Resize  += s => BgfxCore.Resize();
        window.Load += () => BgfxCore.Init(window, opts);
        window.Closing += BgfxCore.Shutdown;

        window.Run();
    }
}