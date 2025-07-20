using Engine.Core.Logging;
using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.Worlds;

public static class BackstageEventLoop
{
    public static void Initialize(Backstage backstage, IWindow window)
    {
        backstage.Window = window;
        try
        {
            backstage.Initialize();
        } catch (Exception e)
        {
            Logger.Error("Failed to initialize backstage: {0}", e);
        }
    }
    public static void ProcessLogicFrame(Backstage backstage, double deltaTime)
    {
        backstage.ProcessLogicFrame(deltaTime);
    }
}
