using Engine.Core.Enum;

namespace Engine.Core.Modules;

public interface IModularHost
{
    public IRootHypervisor Hypervisor { get; set; }
    public void NotifyModuleReloaded(EngineModule module);
    public void NotifyGameplayContextChanged(GameplayContext context);
}

