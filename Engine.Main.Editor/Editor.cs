using System.Diagnostics.CodeAnalysis;
using Engine.Core.EntitySystem;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Hypervisor.Editor.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Microsoft.Build.Locator;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using InputService = Engine.Core.EntitySystem.Services.InputService;
using KillSwitch = Engine.Core.Errors.KillSwitch;

namespace Engine.Main.Editor;

internal static class Editor
{
    private static IWindow MainWindow { get; set; } = null!;
    private static IInputContext WindowInputContext { get; set; } = null!;
    internal static GameplayContext GameplayContext { get; set; } = GameplayContext.Editor;
    
    internal static EditorHostAssembly EditorHostAssembly { get; private set; } = null!;
    internal static PhysicsAssembly PhysicsAssembly { get; private set; } = null!;
    internal static RenderingAssembly RenderingAssembly { get; private set; } = null!;
    internal static UserGameAssembly UserGameAssembly { get; private set; } = null!;
    
    private static List<GuestAssembly> GuestAssemblies { get; set; } = [];
    
    private static EditorHypervisor Hypervisor { get; set; } = null!;
    
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
        if (OperatingSystem.IsMacOS())
            opts.Size /= 2;
        KillSwitch.InstallAvKiller();

        WindowStateManager.TryLoadWindowState(ref opts);
        MainWindow = Window.Create(opts);
        Hypervisor = new EditorHypervisor();
        
        EditorHostAssembly = new EditorHostAssembly();
        PhysicsAssembly = new PhysicsAssembly();
        RenderingAssembly = new RenderingAssembly(MainWindow);
        UserGameAssembly = new UserGameAssembly();
        
        GuestAssemblies =
        [
            EditorHostAssembly,
            PhysicsAssembly,
            RenderingAssembly,
            UserGameAssembly
        ];

        MainWindow.Load += () =>
        {
            // Create input context
            WindowInputContext = MainWindow.CreateInput();
            
            // Setup guest assemblies
            EditorHostAssembly.Init();
            PhysicsAssembly.Init();
            RenderingAssembly.Init();
            UserGameAssembly.Init();
            
            InitBackstage(EditorHostAssembly.Backstage);
            InitBackstage(UserGameAssembly.Backstage);
            
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

        backstage.RootHypervisor = Hypervisor;
        
        BackstageEventLoop.Initialize(backstage, MainWindow);
        RenderingAssembly.Register(backstage);
    }
}

public class EditorHypervisor : IRootHypervisor
{
    public IPhysicsModule? PhysicsModule => Editor.PhysicsAssembly.PhysicsModule;
    public IRenderingModule? RenderingModule => Editor.RenderingAssembly.RenderingModule;

    public void ReloadUserGame()
    {
        Editor.ReloadAssembly(Editor.UserGameAssembly);
    }

    public void SetGameplayContext(GameplayContext context)
    {
        Editor.GameplayContext = context;
        if (Editor.UserGameAssembly.Backstage is not null)
            Editor.UserGameAssembly.Backstage.GameplayContext = context;
        if (Editor.EditorHostAssembly.Backstage is not null)
            Editor.EditorHostAssembly.Backstage.GameplayContext = context;
        if (Editor.RenderingAssembly.RenderingModule is not null)
            Editor.RenderingAssembly.RenderingModule.SetGameplayContext(context);
    }

    public void SetGameplayTimeScale(double timeScale)
    {
        if (Editor.UserGameAssembly.Backstage is not null)
            Editor.UserGameAssembly.Backstage.TimeScale = timeScale;
    }
}
