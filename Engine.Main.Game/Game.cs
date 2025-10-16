using System.Reflection;
using System.Runtime;
using Engine.Core.Communication.Tasks;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules;
using Engine.Main.Game.Modules.Abstract;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Engine.Main.Game;

internal static class Game
{
    private static IWindow MainWindow { get; set; } = null!;
    private static IInputContext MainInputContext { get; set; } = null!;

    private static ShippingGameplayAssembly GameplayAssembly { get; set; } = null!;
    private static ShippingPhysicsAssembly ShippingPhysicsAssembly { get; set; } = null!;
    private static ShippingRenderingAssembly ShippingRenderingAssembly { get; set; } = null!;
    private static ShippingUtilityAssembly ShippingUtilityAssembly { get; set; } = null!;

    private static List<BundledAssembly> GuestAssemblies { get; set; } = [];

    private static void Main()
    {
        var opts = WindowOptions.Default with
        {
            Title = "Custom Engine (Release)",
            Size = new Vector2D<int>(1920, 1080),
            API = new GraphicsAPI(ContextAPI.None, new APIVersion()),
            IsVisible = false,
        };
        if (OperatingSystem.IsMacOS())
            opts.Size /= 2;
        
        MainWindow = Window.Create(opts);
        
        Assembly.Load("User.Game");
        
        GameplayAssembly = new ShippingGameplayAssembly();
        ShippingPhysicsAssembly = new ShippingPhysicsAssembly();
        ShippingRenderingAssembly = new ShippingRenderingAssembly();
        ShippingUtilityAssembly = new ShippingUtilityAssembly();

        GuestAssemblies =
        [
            GameplayAssembly,
            ShippingPhysicsAssembly,
            ShippingRenderingAssembly,
            ShippingUtilityAssembly,
        ];

        GCSettings.LatencyMode = GCLatencyMode.LowLatency;

        MainWindow.Load += () =>
        {
            // Create input context
            MainInputContext = MainWindow.CreateInput();
            
            // First: Rendering to show the splash screen
            ShippingRenderingAssembly.Load();
            MainWindow.IsVisible = true;
            
            // Second: Utility assembly to run DI
            ShippingUtilityAssembly.Load();
            
            // Then: The rest of the owl
            GameplayAssembly.Load();
            ShippingPhysicsAssembly.Load();
            
            ShippingUtilityAssembly.RegisterLaminaRenderers();
            
            // Send the initial reload notification
            foreach (var reloadedAssembly in GuestAssemblies)
            {
                GuestAssemblies.ForEachTry(
                    assembly => assembly.GetHost().NotifyModuleReloaded(reloadedAssembly.Module),
                    (assembly, exception) => Logger.Error($"Failed to notify about module reload: {assembly}", exception)
                );
            }
        };

        MainWindow.Update += deltaTime =>
        {
            MainThreadTask.ExecuteAllQueued();
            
            foreach (var guestAssembly in GuestAssemblies)
            {
                guestAssembly.Update(deltaTime);
            }
        };

        MainWindow.Closing += () =>
        {
            GameplayAssembly.Destroy();
            ShippingPhysicsAssembly.Destroy();
            ShippingRenderingAssembly.Destroy();
            ShippingUtilityAssembly.Destroy();
        };

        MainWindow.Run();
    }

    private static BundledAssembly FindModularAssembly(EngineModule module)
    {
        return module switch
        {
            EngineModule.Gameplay => GameplayAssembly,
            EngineModule.Rendering => ShippingRenderingAssembly,
            EngineModule.Physics => ShippingPhysicsAssembly,
            EngineModule.Utility => ShippingUtilityAssembly,
            EngineModule.Workspace => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module."),
            _ => throw new ArgumentOutOfRangeException(nameof(module), module, "Unmapped engine module.")
        };
    }
    
    public class Hypervisor : IRootHypervisor
    {
        public static Hypervisor Instance { get; } = new();

        public IWindow Window => MainWindow;
        public IInputContext InputContext => MainInputContext;
        public IGameplayHost GameplayModule => GameplayAssembly.HostBackstage;
        public IPhysicsHost PhysicsModule => ShippingPhysicsAssembly.PhysicsModule;
        public IRenderingHost RenderingModule => ShippingRenderingAssembly.RenderingHost;
        public IUtilityHost UtilityModule => ShippingUtilityAssembly.HostBackstage;
        public IWorkspaceHost? WorkspaceModule => null;
        
        public GameplayContext GameplayContext { get; private set; } = GameplayContext.StandalonePlay;

        public void ReloadEngineModule(EngineModule module) {}

        public void SetGameplayContext(GameplayContext context)
        {
            GameplayContext = context;
            GuestAssemblies.ForEachTry(
                assembly => assembly.GetHost().NotifyGameplayContextChanged(context),
                (assembly, exception) => Logger.Error($"Failed to notify about gameplay context change: {assembly}", exception)
            );
        }

        public double GetTimeScale(EngineModule module) => FindModularAssembly(module).TimeScale;
        public void SetTimeScale(EngineModule module, double timeScale) => FindModularAssembly(module).TimeScale = timeScale;
    }
}
