using Engine.Core.Input.Attributes;
using Engine.Core.EntitySystem.Entities;
using Silk.NET.Input;

namespace Engine.Module.Host.Services;

public partial class EditorLoggingService : Service
{
    private WorkspaceHost WorkspaceHost => (WorkspaceHost)Backstage;
    
    [OnKeyInput(Key.F3)]
    protected void OnToggleRendererDebug()
    {
        WorkspaceHost.RenderingModule?.ToggleLogRendering();
    }
}