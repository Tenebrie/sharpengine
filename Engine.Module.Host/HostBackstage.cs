using Engine.Module.Host.Actors;
using Engine.Module.Host.Services;
using Engine.Core.Contracts;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;

namespace Engine.Module.Host;

public partial class HostBackstage : Backstage, IHostBackstage
{
    [OnInit]
    protected void OnInit()
    {
        CreateActor<EditorCamera>();
        RegisterService<EditorInputService>();
        RegisterService<EditorLoggingService>();
        RegisterService<PerformanceMonitoringService>();
    }

    public required IRootHypervisor RootSupervisor { get; set; }
    public Backstage? UserBackstage { get; set; }
}
