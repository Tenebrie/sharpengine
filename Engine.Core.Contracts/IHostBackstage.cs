using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;

namespace Engine.Core.Contracts;

public interface IHostBackstage
{
    IRootHypervisor RootSupervisor { get; set; }
    Backstage? UserBackstage { get; set; }
}
