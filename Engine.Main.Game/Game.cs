using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Windowing;
using Engine.Main.Game.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Main.Shared;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Engine.Main.Game;

internal static class Game
{
    internal static EntryPoint EntryPoint { get; private set; } = null!;
    
    private static void Main()
    {
        RedirectLogsToFile();
        EntryPoint = new EntryPoint();
        EntryPoint.Run();
    }
    
    private static void RedirectLogsToFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var logDir  = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logDir);
        var logPath = Path.Combine(logDir, $"run-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");

        var fs = new FileStream(logPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var tw = new StreamWriter(fs) { AutoFlush = true };

        // Capture Console, Trace, and Debug
        Console.SetOut(tw);
        Console.SetError(tw);
        Trace.Listeners.Clear();
        Trace.Listeners.Add(new TextWriterTraceListener(tw));
    }
}

internal class EntryPoint : IEntryPoint
{
    internal IWindow MainWindow { get; }
    internal IInputContext MainInputContext { get; private set; } = null!;

    internal ShippingGameplayAssembly GameplayAssembly { get; }
    internal ShippingPhysicsAssembly PhysicsAssembly { get; }
    internal ShippingRenderingAssembly RenderingAssembly { get; }
    internal ShippingUtilityAssembly UtilityAssembly { get; }

    internal List<BundledAssembly> HotReloadRoots { get; }
    public Hypervisor Hypervisor { get; }

    internal EntryPoint()
    {
        Hypervisor = new Hypervisor();
        GameplayAssembly = new ShippingGameplayAssembly(this);
        PhysicsAssembly = new ShippingPhysicsAssembly(this);
        RenderingAssembly = new ShippingRenderingAssembly(this);
        UtilityAssembly = new ShippingUtilityAssembly(this);
        HotReloadRoots =
        [
            GameplayAssembly,
            PhysicsAssembly,
            RenderingAssembly,
            UtilityAssembly,
        ];
        
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine (Release)",
            // Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion()),
            IsVisible = false,
        };
        if (OperatingSystem.IsMacOS())
            opts.Size /= 2;
        
        MainWindow = Window.Create(opts);
        Hypervisor.Window = new WindowHandle(MainWindow);
    }

    internal void Run()
    {
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;
        AppContext.SetSwitch("System.GC.Server", true);
        
        var gameLogicThread = new GameLogicThread(this);

        MainWindow.Load += () =>
        {
            MainInputContext = MainWindow.CreateInput();
            
            // var gameAssembly = Assembly.Load("User.Game");
            // First: Rendering to show the splash screen
            RenderingAssembly.Load();
            MainWindow.IsVisible = true;
            
            // Second: Utility assembly to run DI
            Assembly.Load("Engine.Core.Lamina");
            UtilityAssembly.Load();
            
            // Then: The rest of the owl
            GameplayAssembly.Load();
            PhysicsAssembly.Load();
            
            // Send the initial reload notification
            foreach (var reloadedAssembly in HotReloadRoots)
            {
                HotReloadRoots.ForEachTry(
                    assembly => assembly.GetHost().NotifyModuleReloaded(reloadedAssembly.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }
            
            gameLogicThread.Start();
        };

        MainWindow.Closing += () =>
        {
            gameLogicThread.Stop();
            
            GameplayAssembly.Destroy();
            PhysicsAssembly.Destroy();
            RenderingAssembly.Destroy();
            UtilityAssembly.Destroy();
        };

        MainWindow.Run();
    }
    
    IRenderingAssembly IEntryPoint.RenderingAssembly => RenderingAssembly;
    IReadOnlyList<IRootAssembly> IEntryPoint.GuestAssemblies => HotReloadRoots;
    IRootHypervisor IEntryPoint.Hypervisor => Hypervisor;
}

public class Hypervisor : IRootHypervisor
{
    private static EntryPoint EntryPoint => Game.EntryPoint;

    public WindowHandle Window { get; set; } = null!;
    public IInputContext InputContext => EntryPoint.MainInputContext;
    public IGameplayHost GameplayModule => EntryPoint.GameplayAssembly.HostBackstage;
    public IPhysicsHost PhysicsModule => EntryPoint.PhysicsAssembly.PhysicsModule;
    public IRenderingHost RenderingModule => EntryPoint.RenderingAssembly.RenderingHost;
    public IUtilityHost UtilityModule => EntryPoint.UtilityAssembly.HostBackstage;
    public IWorkspaceHost? WorkspaceModule => null;
        
    public GameplayContext GameplayContext { get; private set; } = GameplayContext.StandalonePlay;

    public void ReloadEngineModule(EngineModule module) {}

    public void SetGameplayContext(GameplayContext context)
    {
        GameplayContext = context;
        EntryPoint.HotReloadRoots.ForEachTry(
            assembly => assembly.GetHost().NotifyGameplayContextChanged(context),
            (assembly, exception) => Logger.Error($"Failed to notify about gameplay context change: {assembly}", exception)
        );
    }

    public double GetTimeScale(EngineModule module) => FindModularAssembly(module).TimeScale;
    public void SetTimeScale(EngineModule module, double timeScale) => FindModularAssembly(module).TimeScale = timeScale;
    
    private static BundledAssembly FindModularAssembly(EngineModule module)
    {
        return module switch
        {
            EngineModule.Gameplay => EntryPoint.GameplayAssembly,
            EngineModule.Rendering => EntryPoint.RenderingAssembly,
            EngineModule.Physics => EntryPoint.PhysicsAssembly,
            EngineModule.Utility => EntryPoint.UtilityAssembly,
            EngineModule.Workspace => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module."),
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module.")
        };
    }
}
