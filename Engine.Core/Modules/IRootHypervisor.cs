using Engine.Core.Enum;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Engine.Core.Modules;

public interface IRootHypervisor
{
    public IWindow Window { get; }
    public IInputContext InputContext { get; }
    public IGameplayHost? GameplayModule { get; }
    public IPhysicsHost? PhysicsModule { get; }
    public IRenderingHost? RenderingModule { get; }
    public IWorkspaceHost? WorkspaceModule { get; }
    public IUtilityHost? UtilityModule { get; }
    
    public GameplayContext GameplayContext { get; }
    
    public void ReloadEngineModule(EngineModule module);
    public void SetGameplayContext(GameplayContext context);
    public double GetTimeScale(EngineModule module);
    public void SetTimeScale(EngineModule module, double timeScale);
}
