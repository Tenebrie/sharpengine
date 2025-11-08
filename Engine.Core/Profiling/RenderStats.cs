using Diligent;
using Engine.Core.Common;

namespace Engine.Core.Profiling;

public static class RenderStats
{
    public struct Buffer
    {
        public int NumDrawCalls;
        public int NumLaminaRerenders;
        public int NumLaminaRootDrawCalls;
        public int NumLaminaWidgetDrawCalls;
        public int NumInstancesDrawn;
        public int NumInstancesCulled;
        public DeviceContextStats ImmediateContextStats;
    }

    private static readonly Buffer[] Buffers = new Buffer[2];

    private static long Index => FrameCounter.Current % 2;

    public static int DrawCalls
    {
        get => Buffers[Index].NumDrawCalls;
        set => Buffers[Index].NumDrawCalls = value;
    }
    
    public static int LaminaRerenders
    {
        get => Buffers[Index].NumLaminaRerenders;
        set => Buffers[Index].NumLaminaRerenders = value;
    }
    
    public static int LaminaRootDrawCalls
    {
        get => Buffers[Index].NumLaminaRootDrawCalls;
        set => Buffers[Index].NumLaminaRootDrawCalls = value;
    }
    
    public static int LaminaWidgetDrawCalls
    {
        get => Buffers[Index].NumLaminaWidgetDrawCalls;
        set => Buffers[Index].NumLaminaWidgetDrawCalls = value;
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

    public static void Record(DeviceContextStats diligentStats)
    {
        Buffers[Index].ImmediateContextStats = diligentStats;
    }
    
    public static void Reset()
    {
        Buffers[Index].NumDrawCalls = 0;
        Buffers[Index].NumLaminaRerenders = 0;
        Buffers[Index].NumLaminaRootDrawCalls = 0;
        Buffers[Index].NumLaminaWidgetDrawCalls = 0;
        Buffers[Index].NumInstancesDrawn = 0;
        Buffers[Index].NumInstancesCulled = 0;
    }
}