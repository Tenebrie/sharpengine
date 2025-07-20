using System.Diagnostics.CodeAnalysis;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Editor.HotReload;
using Engine.Editor.HotReload.Abstract;
using Engine.Worlds;
using Engine.Worlds.Entities;
using Microsoft.Build.Locator;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using InputService = Engine.Worlds.Services.InputService;

namespace Engine.Editor;

internal static class Editor
{
    private static IWindow MainWindow { get; set; } = null!;
    private static IInputContext WindowInputContext { get; set; } = null!;
    internal static GameplayContext GameplayContext { get; set; } = GameplayContext.Editor;
    
    internal static EditorHostAssembly EditorHostAssembly { get; set; } = null!;
    internal static RenderingAssembly RenderingAssembly { get; set; } = null!;
    internal static UserlandAssembly UserlandAssembly { get; set; } = null!;
    private static List<GuestAssembly> GuestAssemblies { get; set; } = [];
    
    [SuppressMessage("ReSharper", "ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator")]
    private static void Main()
    {
        MSBuildLocator.RegisterDefaults();
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion())
        };

        WindowStateManager.TryLoadWindowState(ref opts);
        MainWindow = Window.Create(opts);
        
        EditorHostAssembly = new EditorHostAssembly();
        RenderingAssembly = new RenderingAssembly(MainWindow, opts);
        UserlandAssembly = new UserlandAssembly();
        
        GuestAssemblies =
        [
            EditorHostAssembly,
            RenderingAssembly,
            UserlandAssembly
        ];

        MainWindow.Load += () =>
        {
            // Create input context
            WindowInputContext = MainWindow.CreateInput();
            
            // Setup guest assemblies
            RenderingAssembly.Init();
            UserlandAssembly.Init();
            EditorHostAssembly.Init();
            
            InitBackstage(EditorHostAssembly.Backstage);
            InitBackstage(UserlandAssembly.Backstage);
            
            // Save window state for hot reload
            WindowStateManager.SetupAutosaveHandler(MainWindow);
            
            Logger.Info("Engine startup complete.");
        };

        MainWindow.Update += deltaTime =>
        {
            foreach (var guestAssembly in GuestAssemblies)
            {
                var needsReload = guestAssembly.Update(deltaTime);
                if (needsReload)
                    ReloadAssembly(guestAssembly);
            }
        };
        
        MainWindow.Closing += () =>
        {
            WindowStateManager.SaveWindowState(MainWindow);
            WindowStateManager.Cleanup();
        };

        MainWindow.Run();
    }

    /**
     * Reloads the guest assembly and its backstage (if applicable).
     * In other words, perform the hot reload on the guest assembly (user game).
     */
    internal static void ReloadAssembly(GuestAssembly guestAssembly)
    {
        if (guestAssembly.Backstage != null)
            RenderingAssembly.Unregister(guestAssembly.Backstage);
        guestAssembly.Reload();
        if (guestAssembly.Backstage != null)
            InitBackstage(guestAssembly.Backstage);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    
    private static void InitBackstage(Backstage? backstage)
    {
        if (backstage == null)
            return;
        
        var inputHandler = backstage.GetService<InputService>();
        foreach (var inputKeyboard in WindowInputContext.Keyboards)
        {
            inputHandler.BindKeyboardEvents(inputKeyboard);
        }
        foreach (var inputMouse in WindowInputContext.Mice)
        {
            inputHandler.BindMouseEvents(inputMouse);
        }
        
        BackstageEventLoop.Initialize(backstage, MainWindow);
        RenderingAssembly.Register(backstage);
    }
}
