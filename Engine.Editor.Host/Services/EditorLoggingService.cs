using Engine.Input.Attributes;
using Engine.Worlds.Entities;
using Silk.NET.Input;

namespace Engine.Editor.Host.Services;

public class EditorLoggingService : Service
{
    private HostBackstage HostBackstage => (HostBackstage)Backstage;
    
    [OnKeyInput(Key.F3)]
    protected void OnToggleRendererDebug()
    {
        HostBackstage.Renderer?.ToggleLogRendering();
    }
}