using Engine.Editor.Host.Services;
using Engine.Input.Attributes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Utilities;
using Silk.NET.Input;

namespace Engine.Editor.Host;

public class HostBackstage : Backstage
{
    [OnInit]
    protected void OnInit()
    {
        RegisterService<GCMonitoringService>();
    }
    
    [OnKeyInput(Key.F5)]
    protected void OnReload()
    {
        Editor.ReloadGuest();
    }
}