using Engine.Core.Common;
using Engine.Core.Communication.Signals;
using Engine.Core.Enum;
using Engine.Core.Extensions;
using Engine.Core.Windowing;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Engine.Core.Modules;

public interface IRootHypervisor
{
    public WindowHandle Window { get; }
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
