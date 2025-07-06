using Silk.NET.Windowing;

namespace Engine.Worlds;

public class WorldRenderLoop
{
    public static void AttachToWindowLoop(World world, IWindow window)
    {
        window.Load += world.OnInit;
        window.Render += world.ProcessLogicFrame;
        window.Closing += world.OnDestroy;
    }
}