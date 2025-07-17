using Engine.Editor.Host.Actors;
using Engine.Editor.Host.Services;
using Engine.Input.Attributes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Silk.NET.Input;

namespace Engine.Editor.Host;

public class HostBackstage : Backstage
{
    [OnInit]
    protected void OnInit()
    {
        CreateActor<EditorCamera>();
        RegisterService<EditorInputService>();
        RegisterService<GCMonitoringService>();
    }
    
    [OnKeyInput(Key.F5)]
    protected void OnReload()
    {
        // TODO: Reimplement this
        // Editor.ReloadGuest();
    }
}