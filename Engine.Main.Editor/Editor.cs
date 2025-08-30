using System.Runtime;
using Engine.Core.Communication.Tasks;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Editor.Modules.Compiler;
using Microsoft.Build.Locator;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Main.Editor;

internal static class Editor
{
    private static IWindow MainWindow { get; set; } = null!;
    private static IInputContext MainInputContext { get; set; } = null!;

    internal static GameplayAssembly GameplayAssembly { get; private set; } = null!;
    internal static PhysicsAssembly PhysicsAssembly { get; private set; } = null!;
    internal static RenderingAssembly RenderingAssembly { get; private set; } = null!;
    internal static WorkspaceAssembly WorkspaceAssembly { get; private set; } = null!;

    private static List<ModularAssembly> GuestAssemblies { get; set; } = [];

    private static void Main()
    {
        MSBuildLocator.RegisterDefaults();
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion()),
            IsVisible = false,
        };
        if (OperatingSystem.IsMacOS())
            opts.Size /= 2;
        
        GuestAssemblyLoader.CleanTempFolder();

        WindowStateManager.TryLoadWindowState(ref opts);
        MainWindow = Window.Create(opts);

        WorkspaceAssembly = new WorkspaceAssembly();
        PhysicsAssembly = new PhysicsAssembly();
        RenderingAssembly = new RenderingAssembly();
        GameplayAssembly = new GameplayAssembly();

        GuestAssemblies =
        [
            WorkspaceAssembly,
            GameplayAssembly,
            PhysicsAssembly,
            RenderingAssembly,
        ];
        
        GCSettings.LatencyMode = GCLatencyMode.LowLatency;

        MainWindow.Load += () =>
        {
            // Create input context
            MainInputContext = MainWindow.CreateInput();

            // Setup rendering first
            RenderingAssembly.Load();
            RenderingAssembly.RenderingHost?.RenderEngineLoadingScreen();

            MainWindow.IsVisible = true;
            
            // Setup guest assemblies
            GameplayAssembly.Load();
            PhysicsAssembly.Load();
            WorkspaceAssembly.Load();
            
            foreach (var reloadedAssembly in GuestAssemblies)
            {
                GuestAssemblies.ForEachTry(
                    assembly => assembly.GetHost()?.NotifyModuleReloaded(reloadedAssembly.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }

            // Save window state for hot reload
            WindowStateManager.SetupAutosaveHandler(MainWindow);

            Logger.Info("Engine startup complete.");
        };

        MainWindow.Update += deltaTime =>
        {
            MainThreadTask.ExecuteAllQueued();
            
            foreach (var libraryAssembly in AssemblyRepository.LibraryAssemblies.Values)
            {
                libraryAssembly.Update(deltaTime);
            }
            foreach (var guestAssembly in GuestAssemblies)
            {
                guestAssembly.Update(deltaTime);
            }

            var allAssemblies = AssemblyRepository.LibraryAssemblies.Values.ToList();
            allAssemblies.Add(RenderingAssembly);
            allAssemblies.Add(GameplayAssembly);
            allAssemblies.Add(PhysicsAssembly);
            allAssemblies.Add(WorkspaceAssembly);

            var wantingBuild = allAssemblies.Where(a => a.NeedsRebuild()).ToList();
            if (wantingBuild.Count > 0 && !allAssemblies.Any(a => a.Loader.DebounceTimer > 0.0))
            {
                AssemblyRepository.RebuildCascading(wantingBuild);
                return;
            }
            
            if (allAssemblies.Any(assembly => assembly.Loader.IsCompiling || assembly.Loader.HasErrors))
                return;

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
                GuestAssemblies.ForEachTry(
                    assembly => assembly.GetHost()?.NotifyModuleReloaded(reloadedModule.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }
        };

        MainWindow.Closing += () =>
        {
            WindowStateManager.SaveWindowState(MainWindow);

            PhysicsAssembly.Destroy();
            GameplayAssembly.Destroy();
            WorkspaceAssembly.Destroy();
            RenderingAssembly.Destroy();

            WindowStateManager.Cleanup();
        };

        MainWindow.Run();
    }

    /**
     * Reloads the guest assembly and its backstage (if applicable).
     * In other words, perform the hot reload on the guest assembly (user game).
     */
    private static void ReloadAssembly(ModularAssembly modularAssembly)
    {
        modularAssembly.Reload();

        GuestAssemblies.ForEachTry(
            assembly => assembly.GetHost()?.NotifyModuleReloaded(modularAssembly.Module),
            (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
        );
    }

    private static ModularAssembly FindModularAssembly(EngineModule module)
    {
        return module switch
        {
            EngineModule.Gameplay => GameplayAssembly,
            EngineModule.Rendering => RenderingAssembly,
            EngineModule.Physics => PhysicsAssembly,
            EngineModule.Workspace => WorkspaceAssembly,
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module.")
        };
    }
    
    public class Hypervisor : IRootHypervisor
    {
        public static Hypervisor Instance { get; } = new();

        public IWindow Window => MainWindow;
        public IInputContext InputContext => MainInputContext;
        public IGameplayHost? GameplayModule => GameplayAssembly.HostBackstage;
        public IPhysicsHost? PhysicsModule => PhysicsAssembly.PhysicsModule;
        public IRenderingHost? RenderingModule => RenderingAssembly.RenderingHost;
        public IWorkspaceHost? WorkspaceModule => WorkspaceAssembly.HostBackstage;
        
        public GameplayContext GameplayContext { get; private set; } = GameplayContext.Editor;

        public void ReloadEngineModule(EngineModule module) => ReloadAssembly(FindModularAssembly(module));

        public void SetGameplayContext(GameplayContext context)
        {
            GameplayContext = context;
            GuestAssemblies.ForEachTry(
                assembly => assembly.GetHost()?.NotifyGameplayContextChanged(context),
                (assembly, exception) => Logger.Error($"Failed to notify about gameplay context change: {assembly}", exception)
            );
        }

        public double GetTimeScale(EngineModule module) => FindModularAssembly(module).TimeScale;
        public void SetTimeScale(EngineModule module, double timeScale) => FindModularAssembly(module).TimeScale = timeScale;
    }
}
