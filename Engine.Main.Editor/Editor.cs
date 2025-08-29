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
using KillSwitch = Engine.Core.Errors.KillSwitch;

namespace Engine.Main.Editor;

internal static class Editor
{
    private static IWindow MainWindow { get; set; } = null!;
    private static IInputContext MainInputContext { get; set; } = null!;

    internal static GameplayAssembly GameplayAssembly { get; set; } = null!;
    internal static PhysicsAssembly PhysicsAssembly { get; set; } = null!;
    internal static RenderingAssembly RenderingAssembly { get; set; } = null!;
    internal static WorkspaceAssembly WorkspaceAssembly { get; set; } = null!;

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
        KillSwitch.InstallAvKiller();
        
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
            
            // foreach (var node in AssemblyRepository.References.Values)
            // {
            //     Console.WriteLine(node.Name + ": ");
            //     foreach (var nodeDep in node.Dependencies)
            //     {
            //         Console.WriteLine(" <- " + nodeDep);
            //     }
            //     foreach (var nodeDepOf in node.IsDependencyOf)
            //     {
            //         Console.WriteLine(" -> " + nodeDepOf);
            //     }
            // }
            
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
                guestAssembly.Update(deltaTime * guestAssembly.TimeScale);
            }

            var allAssemblies = AssemblyRepository.LibraryAssemblies.Values.ToList();
            allAssemblies.Add(RenderingAssembly);
            allAssemblies.Add(GameplayAssembly);
            allAssemblies.Add(PhysicsAssembly);
            allAssemblies.Add(WorkspaceAssembly);
            
            bool dependenciesInvalidated;
            do
            {
                dependenciesInvalidated = false;
                foreach (var libraryAssembly in allAssemblies)
                {
                    var reloadNeeded = libraryAssembly.NeedsReload();
                    dependenciesInvalidated |= reloadNeeded;
                    if (!reloadNeeded)
                        continue;
                    dependenciesInvalidated = AssemblyRepository.InvalidateDependencies(libraryAssembly.Name);
                }
            } while (dependenciesInvalidated);
            
            var reloadedModules = AssemblyRepository.ReloadAllAwaiting();
            foreach (var reloadedModule in reloadedModules)
            {
                reloadedModule.SkipNextUpdate = true;
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
