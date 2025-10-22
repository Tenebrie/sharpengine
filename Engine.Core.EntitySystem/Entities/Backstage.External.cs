using Engine.Core.EntitySystem.Services;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling;
using Engine.Core.Windowing;
using JetBrains.Annotations;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Entities;

[PublicAPI]
public partial class Backstage : IBackstage
{
    public string Name { get; set; } = "Backstage";
    public IRootHypervisor Hypervisor { get; set; }

    public WindowHandle Window => Hypervisor.Window;
    public IPhysicsHost? PhysicsModule => Hypervisor.PhysicsModule;
    public IRenderingHost? RenderingModule => Hypervisor.RenderingModule;
    public IUtilityHost? UtilityModule => Hypervisor.UtilityModule;
    public IWorkspaceHost? WorkspaceModule => Hypervisor.WorkspaceModule;
    public GameplayContext GameplayContext => Hypervisor.GameplayContext;
    
    public void StartupInitialize()
    {
        try
        {
            Initialize();
            var inputHandler = GetService<InputService>();
            foreach (var inputKeyboard in Hypervisor.InputContext.Keyboards)
            {
                inputHandler.BindKeyboardEvents(inputKeyboard);
            }
            foreach (var inputMouse in Hypervisor.InputContext.Mice)
            {
                inputHandler.BindMouseEvents(inputMouse);
            }
            foreach (var inputGamepad in Hypervisor.InputContext.Gamepads)
            {
                inputHandler.BindGamepadEvents(inputGamepad);
            }
        } catch (Exception e)
        {
            Logger.Error("Failed to initialize backstage", e);
        }
    }
    
    public void TriggerLogicFrameUpdate(double deltaTime)
    {
        if (deltaTime > 0.5)
            return;
        var stopwatch = Profiler.Start();
        ProcessLogicFrame(deltaTime);
        stopwatch.StopAndReport(GetType(), ProfilingContext.BackstageUpdate);
    }
}

public partial class GameplayHostBackstage : Backstage, IGameplayHost;