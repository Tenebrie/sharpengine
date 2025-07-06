using Engine.Rendering.Bgfx;
using Engine.Worlds;
using Game.User;
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

        // Load user settings here
        var userSettings = new UserSettings();
        
        var world = (World)Activator.CreateInstance(userSettings.WorldType)!;

        var window = Window.Create(opts);
        window.Render += (delta) => BgfxCore.RenderSingleFrame(delta, world);
        window.Resize  += BgfxCore.Resize;
        window.Load += () =>
        {
            BgfxCore.Init(window, opts);
        };
        window.Closing += BgfxCore.Shutdown;
        
        WorldRenderLoop.AttachToWindowLoop(world, window);

        window.Run();
    }
}