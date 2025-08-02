using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem;

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
            Logger.Error("Failed to initialize backstage", e);
        }
    }

    public static void InitializeStaticAtomFields()
    {
        
    }
    
    public static void ProcessLogicFrame(Backstage backstage, double deltaTime)
    {
        backstage.ProcessLogicFrame(deltaTime);
    }
}
