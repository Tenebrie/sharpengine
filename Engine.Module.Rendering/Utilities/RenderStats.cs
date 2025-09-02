namespace Engine.Module.Rendering.Utilities;

public static class RenderStats
{
    public static int DrawCalls { get; set; }
    public static int InstancesDrawn { get; set; }
    public static int InstancesCulled { get; set; }
    
    public static void Reset()
    {
        DrawCalls = 0;
        InstancesDrawn = 0;
        InstancesCulled = 0;
    }
}