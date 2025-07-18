using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Editor.Host.Services;

public class PerformanceMonitoringService : Service
{
    private int lastGen0 = 0;
    private int lastGen1 = 0;
    private int lastGen2 = 0; 

    [OnTimer(Seconds = 1)]
    protected void OnCheckGC()   
    { 
        // baseline snapshot
        var g0 = GC.CollectionCount(0); 
        var g1 = GC.CollectionCount(1);
        var g2 = GC.CollectionCount(2);

        // Console.WriteLine($"GC/f  g0:{g0 - lastGen0}  g1:{g1 - lastGen1}  g2:{g2 - lastGen2}");
 
        lastGen0 = g0;
        lastGen1 = g1;  
        lastGen2 = g2;
    }

    [OnTimer(Seconds = 3)]
    protected void OnCheckCPU()
    {
        // Profiler.GenerateReport();
        Profiler.Reset();
    }
}