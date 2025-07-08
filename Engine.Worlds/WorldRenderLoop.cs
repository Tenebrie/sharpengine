using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.Worlds;

public static class WorldRenderLoop
{
    public static void AttachToWindowLoop(Backstage backstage, IWindow window)
    {
        window.Load += backstage.InitializeLifecycle;
        window.Render += backstage.ProcessLogicFrame;
        window.Closing += backstage.FreeImmediately;
    }
}