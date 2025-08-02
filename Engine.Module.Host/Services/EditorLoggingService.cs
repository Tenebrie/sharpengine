using Engine.Core.Input.Attributes;
using Engine.Core.EntitySystem.Entities;
using Silk.NET.Input;

namespace Engine.Module.Host.Services;

public partial class EditorLoggingService : Service
{
    private HostBackstage HostBackstage => (HostBackstage)Backstage;
    
    [OnKeyInput(Key.F3)]
    protected void OnToggleRendererDebug()
    {
        HostBackstage.RenderingModule?.ToggleLogRendering();
    }
}