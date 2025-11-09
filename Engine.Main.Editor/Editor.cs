using System.Runtime;
using Engine.Core.Communication.Tasks;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Windowing;
using Engine.Main.Editor.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Editor.Modules.Compiler;
using Engine.Main.Shared;
using Microsoft.Build.Locator;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Main.Editor;

internal static class Editor
{
    internal static EntryPoint EntryPoint { get; private set; } = null!;
    
    private static void Main()
    {
        MSBuildLocator.RegisterDefaults();
        EntryPoint = new EntryPoint();
        EntryPoint.Run();
    }
}

internal class EntryPoint : IEntryPoint
{
    internal IWindow MainWindow { get; }
    internal IInputContext MainInputContext { get; private set; } = null!;

    internal GameplayAssembly GameplayAssembly { get; }
    internal PhysicsAssembly PhysicsAssembly { get; }
    internal RenderingAssembly RenderingAssembly { get; }
    internal UtilityAssembly UtilityAssembly { get; }
    internal WorkspaceAssembly WorkspaceAssembly { get; }

    internal List<ModularAssembly> HotReloadRoots { get; }
    private Hypervisor Hypervisor { get; }
    
    internal EntryPoint()
    {
        Hypervisor = new Hypervisor();
        GameplayAssembly = new GameplayAssembly(this);
        PhysicsAssembly = new PhysicsAssembly(this);
        RenderingAssembly = new RenderingAssembly(this);
        UtilityAssembly = new UtilityAssembly(this);
        WorkspaceAssembly = new WorkspaceAssembly(this);

        HotReloadRoots =
        [
            GameplayAssembly,
            PhysicsAssembly,
            RenderingAssembly,
            UtilityAssembly,
            WorkspaceAssembly,
        ];
        
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion()),
            IsVisible = false
        };
        if (OperatingSystem.IsMacOS())
            opts.Size /= 2;

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Logger.Fatal($"{e.ExceptionObject}");
        };
        
        GuestAssemblyLoader.CleanTempFolder();

        WindowStateManager.TryLoadWindowState(ref opts);
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
            
            // First: Rendering to show the splash screen
            RenderingAssembly.Load();
            MainWindow.IsVisible = true;
 
            // Second: Utility assembly to run DI
            UtilityAssembly.Load();
            
            // Then: The rest of the owl
            GameplayAssembly.Load();
            PhysicsAssembly.Load();
            WorkspaceAssembly.Load();
            
            // Send the initial reload notification
            foreach (var reloadedAssembly in HotReloadRoots)
            {
                HotReloadRoots.ForEachTry(
                    assembly => assembly.GetHost()?.NotifyModuleReloaded(reloadedAssembly.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }

            WindowStateManager.SetupAutosaveHandler(MainWindow);
            
            gameLogicThread.Start();

            Logger.Info("Engine startup complete.");
        };

        var deltaTotal = 0.0;
        
        /*
         * Internal editor update loop to handle hot-reloading
         * See GameLogicThread for the main update loop
         */
        MainWindow.Update += deltaTime =>
        {
            deltaTotal += deltaTime;
            if (deltaTotal < 0.003)
                return;
            
            MainThreadTask.ExecuteAllQueued();
            
            if (deltaTotal < 0.05)
                return;
            
            var allAssemblies = AssemblyRepository.LibraryAssemblies.Values.ToList();
            foreach (var libraryAssembly in AssemblyRepository.LibraryAssemblies.Values)
                libraryAssembly.Update(deltaTotal);
            deltaTotal = 0.0;
            
            allAssemblies.Add(RenderingAssembly);
            allAssemblies.Add(GameplayAssembly);
            allAssemblies.Add(PhysicsAssembly);
            allAssemblies.Add(UtilityAssembly);
            allAssemblies.Add(WorkspaceAssembly);

            var wantingBuild = allAssemblies.Where(a => a.NeedsRebuild()).ToList();
            if (wantingBuild.Count > 0 && !allAssemblies.Any(a => a.Loader.DebounceTimer > 0.0))
            {
                AssemblyRepository.RebuildCascading(wantingBuild);
            }

            var anyBlocked = allAssemblies.Any(assembly => assembly.Loader.IsCompiling || assembly.Loader.HasErrors);
            var anyNeedsReload = allAssemblies.Any(assembly => assembly.NeedsReload());
            if (anyBlocked || !anyNeedsReload)
                return;
            
            gameLogicThread.Pause();

            int awaitingCount;
            do
            {
                awaitingCount = AssemblyRepository.AssembliesAwaitingReload.Count;
                foreach (var libraryAssembly in allAssemblies)
                {
                    var reloadNeeded = libraryAssembly.NeedsReload();
                    if (!reloadNeeded)
                        continue;
                    AssemblyRepository.InvalidateDependencies(libraryAssembly.Name);
                }
            } while (awaitingCount < AssemblyRepository.AssembliesAwaitingReload.Count);
            
            var reloadedModules = AssemblyRepository.ReloadAllAwaiting();
            foreach (var reloadedModule in reloadedModules)
            {
                HotReloadRoots.ForEachTry(
                    assembly => assembly.GetHost()?.NotifyModuleReloaded(reloadedModule.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }
            
            gameLogicThread.Resume();
        };

        MainWindow.Closing += () =>
        {
            WindowStateManager.SaveWindowState(MainWindow);
            
            gameLogicThread.Stop();

            GameplayAssembly.Destroy();
            PhysicsAssembly.Destroy();
            RenderingAssembly.Destroy();
            UtilityAssembly.Destroy();
            WorkspaceAssembly.Destroy();

            WindowStateManager.Cleanup();
        };

        MainWindow.Run();
    }
    
    IRenderingAssembly IEntryPoint.RenderingAssembly => RenderingAssembly;
    IReadOnlyList<IRootAssembly> IEntryPoint.GuestAssemblies => HotReloadRoots;
    IRootHypervisor IEntryPoint.Hypervisor => Hypervisor;
}

public class Hypervisor : IRootHypervisor
{
    private static EntryPoint EntryPoint => Editor.EntryPoint;

    public WindowHandle Window { get; set; } = null!;
    public IInputContext InputContext => EntryPoint.MainInputContext;
    public IGameplayHost? GameplayModule => EntryPoint.GameplayAssembly.HostBackstage;
    public IPhysicsHost? PhysicsModule => EntryPoint.PhysicsAssembly.PhysicsModule;
    public IRenderingHost? RenderingModule => EntryPoint.RenderingAssembly.RenderingHost;
    public IUtilityHost? UtilityModule => EntryPoint.UtilityAssembly.HostBackstage;
    public IWorkspaceHost? WorkspaceModule => EntryPoint.WorkspaceAssembly.HostBackstage;
        
    public GameplayContext GameplayContext { get; private set; } = GameplayContext.Editor;

    public void ReloadEngineModule(EngineModule module) => ReloadAssembly(FindModularAssembly(module));

    public void SetGameplayContext(GameplayContext context)
    {
        GameplayContext = context;
        EntryPoint.HotReloadRoots.ForEachTry(
            assembly => assembly.GetHost()?.NotifyGameplayContextChanged(context),
            (assembly, exception) => Logger.Error($"Failed to notify about gameplay context change: {assembly}", exception)
        );
    }

    public double GetTimeScale(EngineModule module) => FindModularAssembly(module).TimeScale;
    public void SetTimeScale(EngineModule module, double timeScale) => FindModularAssembly(module).TimeScale = timeScale;
    
    private static ModularAssembly FindModularAssembly(EngineModule module)
    {
        return module switch
        {
            EngineModule.Gameplay => EntryPoint.GameplayAssembly,
            EngineModule.Rendering => EntryPoint.RenderingAssembly,
            EngineModule.Physics => EntryPoint.PhysicsAssembly,
            EngineModule.Utility => EntryPoint.UtilityAssembly,
            EngineModule.Workspace => EntryPoint.WorkspaceAssembly,
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module.")
        };
    }
    
    private static void ReloadAssembly(ModularAssembly modularAssembly)
    {
        modularAssembly.Reload();

        EntryPoint.HotReloadRoots.ForEachTry(
            assembly => assembly.GetHost()?.NotifyModuleReloaded(modularAssembly.Module),
            (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
        );
    }
}
