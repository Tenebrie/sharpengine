using Engine.Editor.Host;
using Engine.Editor.HotReload;
using Engine.Rendering;
using Engine.User.Contracts;
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
    private static RenderingCore Renderer { get; set; } = null!;
    private static IInputContext WindowInputContext { get; set; } = null!;
    private static HostBackstage HostBackstage { get; set; } = null!;
    private static GameAssemblyHost GuestAssemblyHost { get; set; } = null!;
    private static Backstage? GuestBackstage { get; set; }
    private static IEngineSettings<Backstage>? GuestSettings { get; set; }
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
        Renderer = new RenderingCore();
        GuestAssemblyHost = new GameAssemblyHost("User.Game");

        MainWindow.Load += () =>
        {
            // Initialize bgfx
            Renderer.Initialize(MainWindow, opts);
            
            // Create input context
            WindowInputContext = MainWindow.CreateInput();
            
            // Save window state for hot reload
            WindowStateManager.SetupAutosaveHandler(MainWindow);
            
            InitHost();
            InitGuest();
        };

        MainWindow.Update += deltaTime =>
        {
            if (GuestAssemblyHost.IsCompiling)
            {
                // Do nothing
            }
            else if (GuestAssemblyHost.IsAssemblyDirty)
            {
                GuestAssemblyHost.BuildGuestAsync();
            }
            else if (GuestAssemblyHost.AssemblyAwaitingReload)
            {
                ReloadGuest();
            }
            BackstageEventLoop.ProcessLogicFrame(HostBackstage, deltaTime);
            if (GuestBackstage != null)
                BackstageEventLoop.ProcessLogicFrame(GuestBackstage, deltaTime);
        };
        
        MainWindow.Closing += () =>
        {
            WindowStateManager.SaveWindowState(MainWindow);
            WindowStateManager.Cleanup();
        };

        MainWindow.Run();
    }

    internal static void ReloadGuest()
    {
        GuestAssemblyHost.AssemblyAwaitingReload = false;
        if (GuestBackstage != null)
        {
            Renderer.Unregister(GuestBackstage);
            GuestBackstage.FreeImmediately();
        }
        GuestSettings = null;
        GuestBackstage = null;
        GuestAssemblyHost.UnloadCurrent();
        InitGuest();
    }

    private static void InitHost()
    {
        HostBackstage = new HostBackstage();
        InitBackstage(HostBackstage);
        HostBackstage.Name = "host-" + Guid.NewGuid();
    }
    
    private static void InitGuest()
    {
        GuestSettings = GuestAssemblyHost.Load();
        GuestBackstage = (Backstage)Activator.CreateInstance(GuestSettings.MainBackstage)!;
        InitBackstage(GuestBackstage);
        GuestBackstage.Name = "guest-" + Guid.NewGuid();
    }

    private static void InitBackstage(Backstage backstage)
    {
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
        Renderer.Register(backstage);
    }
}