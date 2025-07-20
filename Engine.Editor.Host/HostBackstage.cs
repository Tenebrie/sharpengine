using Engine.Editor.Host.Actors;
using Engine.Editor.Host.Services;
using Engine.User.Contracts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Editor.Host;

public class HostBackstage : Backstage, IEditorHostContract
{
    [OnInit]
    protected void OnInit()
    {
        CreateActor<EditorCamera>();
        RegisterService<EditorInputService>();
        RegisterService<EditorLoggingService>();
        RegisterService<PerformanceMonitoringService>();
    }

    public required IEditorContract Editor { get; set; }
    public Backstage? UserBackstage { get; set; }
    public IRendererContract? Renderer { get; set; }
}
