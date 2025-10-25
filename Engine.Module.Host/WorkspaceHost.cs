using Engine.Module.Host.Actors;
using Engine.Module.Host.Services;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Modules;

namespace Engine.Module.Host;

public partial class WorkspaceHost : Backstage, IWorkspaceHost
{
    [OnReady] 
    protected void OnReady()
    {
        CreateActor<EditorCamera>();
        CreateActor<EditorPerformanceWidget>();
        RegisterService<EditorInputService>();
        RegisterService<EditorLoggingService>();
    }
}
