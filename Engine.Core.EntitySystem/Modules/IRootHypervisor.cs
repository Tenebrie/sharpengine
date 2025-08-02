using Engine.Core.Enum;

namespace Engine.Core.EntitySystem.Modules;

public interface IRootHypervisor
{
    public IPhysicsModule? PhysicsModule { get; }
    public IRenderingModule? RenderingModule { get; }
    
    public void ReloadUserGame();
    public void SetGameplayContext(GameplayContext context);
    public void SetGameplayTimeScale(double timeScale);
}
