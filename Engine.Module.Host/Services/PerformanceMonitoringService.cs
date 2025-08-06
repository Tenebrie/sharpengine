using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Module.Host.Services;

public partial class PerformanceMonitoringService : Service
{
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

        var g0Diff = g0 - _lastGen0;
        var g1Diff = g1 - _lastGen1;
        var g2Diff = g2 - _lastGen2;
        // if (g0Diff > 1 || g1Diff > 1 || g2Diff > 1)
            // Logger.Debug($"GC Report:  g0: {g0Diff}  g1: {g1Diff}  g2: {g2Diff}");
        
        if (g0Diff > 1 || g1Diff > 1 || g2Diff > 1)
            Logger.ShowPersistent(this, $"GC Warning: {g0Diff + g1Diff + g2Diff} collections per second.");
        else
            Logger.ClearPersistent(this);
 
        _lastGen0 = g0;
        _lastGen1 = g1;
        _lastGen2 = g2;
    }

    [OnTimer(Seconds = 3)]
    protected void OnCheckCPU()
    {
        Profiler.GenerateReport();
        Profiler.Reset();
    }
}