using Engine.Core.Common;

namespace Engine.Core.Profiling;

public static class RenderStats
{
    public struct Buffer
    {
        public int NumDrawCalls;
        public int NumInstancesDrawn;
        public int NumInstancesCulled;
    }

    private static readonly Buffer[] Buffers = new Buffer[2];

    private static long Index => FrameCounter.Current % 2;

    public static int DrawCalls
    {
        get => Buffers[Index].NumDrawCalls;
        set => Buffers[Index].NumDrawCalls = value;
    }

    public static int InstancesDrawn
    {
        get => Buffers[Index].NumInstancesDrawn;
        set => Buffers[Index].NumInstancesDrawn = value;
    }

    public static int InstancesCulled
    {
        get => Buffers[Index].NumInstancesCulled;
        set => Buffers[Index].NumInstancesCulled = value;
    }
    
    public static Buffer GetPreviousFrameStats()
    {
        var previousIndex = 1 - FrameCounter.Current % 2;
        return Buffers[previousIndex];
    }

    public static void Reset()
    {
        Buffers[Index].NumDrawCalls = 0;
        Buffers[Index].NumInstancesDrawn = 0;
        Buffers[Index].NumInstancesCulled = 0;
    }
}