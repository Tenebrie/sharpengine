using Engine.Rendering;
using Engine.Worlds;
using Engine.Worlds.Entities;
using Game.User;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Editor;

internal static class Editor
{
    private static void Main()
    {
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion())
        };

        WindowStateManager.TryLoadWindowState(ref opts);
        
        // TODO: Load userland settings here (or reflections idk)
        var userSettings = new UserSettings();
        
        var window = Window.Create(opts);
        var backstage = (Backstage)Activator.CreateInstance(userSettings.BackstageType)!;
        var renderer = new RenderingCore();
        
        window.Load += () =>
        {
            renderer.OnInit(window, opts);
            
            // Save window state for hot reload
            WindowStateManager.SetupAutosaveHandler(window);
        };
        
        BackstageEventLoop.ConnectToWindowEvents(backstage, window);
        renderer.ConnectToWindowEvents(backstage, window);
        
        window.Closing += () =>
        {
            WindowStateManager.SaveWindowState(window);
            WindowStateManager.Cleanup();
        };

        window.Run();
    }
}