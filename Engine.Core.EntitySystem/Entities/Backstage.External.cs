using Engine.Core.EntitySystem.Services;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Entities;

public partial class Backstage : IBackstage
{
    public string Name { get; set; } = "Backstage";
    public IRootHypervisor Hypervisor { get; set; }

    public IWindow Window => Hypervisor.Window;
    public IPhysicsHost? PhysicsModule => Hypervisor.PhysicsModule;
    public IRenderingHost? RenderingModule => Hypervisor.RenderingModule;
    public GameplayContext GameplayContext => Hypervisor.GameplayContext;
    
    public void StartupInitialize()
    {
        var inputHandler = GetService<InputService>();
        foreach (var inputKeyboard in Hypervisor.InputContext.Keyboards)
        {
            inputHandler.BindKeyboardEvents(inputKeyboard);
        }
        foreach (var inputMouse in Hypervisor.InputContext.Mice)
        {
            inputHandler.BindMouseEvents(inputMouse);
        }
        
        try
        {
            Initialize();
        } catch (Exception e)
        {
            Logger.Error("Failed to initialize backstage", e);
        }
    }
    
    public void TriggerLogicFrameUpdate(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        ProcessLogicFrame(deltaTime);
        stopwatch.StopAndReport(GetType(), ProfilingContext.BackstageUpdate);
    }
}

public partial class GameplayHostBackstage : Backstage, IGameplayHost;