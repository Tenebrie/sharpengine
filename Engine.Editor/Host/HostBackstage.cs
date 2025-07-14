using Engine.Input.Attributes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Silk.NET.Input;

namespace Engine.Editor.Host;

public class HostBackstage : Backstage
{
    [OnKeyInput(Key.F5)]
    protected void OnReload()
    {
        Editor.ReloadGuest();
    }
}