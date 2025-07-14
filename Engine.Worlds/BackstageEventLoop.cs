using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.Worlds;

public static class BackstageEventLoop
{
    public static void Initialize(Backstage backstage, IWindow window)
    {
        backstage.Initialize();
        backstage.Window = window;
    }
    public static void ProcessLogicFrame(Backstage backstage, double deltaTime)
    {
        backstage.ProcessLogicFrame(deltaTime);
    }
}