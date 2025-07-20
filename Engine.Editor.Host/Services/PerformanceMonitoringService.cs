using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Editor.Host.Services;

public class PerformanceMonitoringService : Service
{
    private const bool LoggingEnabled = 1 < 2;

    private int _lastGen0 = 0;
    private int _lastGen1 = 0;
    private int _lastGen2 = 0; 

    [OnTimer(Seconds = 1)]
    protected void OnCheckGC()   
    { 
        // baseline snapshot
        var g0 = GC.CollectionCount(0); 
        var g1 = GC.CollectionCount(1);
        var g2 = GC.CollectionCount(2);

        if (LoggingEnabled)
            Logger.Debug($"GC/f  g0:{g0 - _lastGen0}  g1:{g1 - _lastGen1}  g2:{g2 - _lastGen2}");
 
        _lastGen0 = g0;
        _lastGen1 = g1;  
        _lastGen2 = g2;
    }

    [OnTimer(Seconds = 3)]
    protected void OnCheckCPU()
    {
        // Profiler.GenerateReport();
        Profiler.Reset();
    }
}