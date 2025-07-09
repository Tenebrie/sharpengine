using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.Worlds;

public static class BackstageEventLoop
{
    public static void ConnectToWindowEvents(Backstage backstage, IWindow window)
    {
        backstage.Window = window;
        window.Load += backstage.InitializeLifecycle;
        window.Render += backstage.ProcessLogicFrame;
        window.Closing += backstage.FreeImmediately;
    }
}