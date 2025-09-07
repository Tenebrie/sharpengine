namespace Engine.Core.Profiling;

[Flags]
public enum ProfilingContext
{
    Unknown = 0,
    PhysicsUpdate = 1 << 0,
    OnCreateCallback = 1 << 1,
    OnReadyCallback = 1 << 2,
    OnUpdateCallback = 1 << 3,
    OnDestroyCallback = 1 << 4,
    BackstageUpdate = 1 << 5,
    RenderingFullFrame = 1 << 6,
    RenderingCollectAtoms = 1 << 7,
    RenderingCombineRequests = 1 << 8,
    RenderingSubmitAtoms = 1 << 9,
    RenderingDebugLog = 1 << 10,
    RenderingDebugFramerate = 1 << 11,
    RenderingDebugProfiler = 1 << 12,
    RenderingGpuWait = 1 << 13,
}

public static class Profiler
{
    public static IProfiler? Implementation { get; set; }
    
    public static int CurrentBufferIndex => Implementation?.CurrentBufferIndex ?? 0;

    private static DummyProfilingStopwatch DummyStopwatch { get; } = new();
    public static IProfilingStopwatch Start()
    {
        if (Implementation == null)
            return DummyStopwatch;
        return Implementation.Start();
    }

    private static IProfilerEntry[] DummyEntries { get; } = [];
    public static IProfilerEntry[] Query(ProfilingContext context)
    {
        if (Implementation == null)
            return DummyEntries;
        return Implementation.Query(context);
    }

    public static IProfilerEntry[] QueryWorstOffenders()
    {
        if (Implementation == null)
            return DummyEntries;
        return Implementation.QueryWorstOffenders();
    }

    public static void SwapBuffers()
    {
        Implementation?.SwapBuffers();
    }
}

public interface IProfiler
{
    public int CurrentBufferIndex { get; }

    public IProfilingStopwatch Start();

    public IProfilerEntry[] Query(ProfilingContext context);

    public IProfilerEntry[] QueryWorstOffenders();

    public void SwapBuffers();
}

public interface IProfilingStopwatch : IDisposable
{
    public void StopAndReport(object owner);
    public void StopAndReport(Type owner, ProfilingContext context);
    public void StopAndReportMethod(Type owner, string methodName);
}

public class DummyProfilingStopwatch : IProfilingStopwatch
{
    public void StopAndReport(object owner) {}
    public void StopAndReport(Type owner, ProfilingContext context) {}
    public void StopAndReportMethod(Type owner, string methodName) {}

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public interface IProfilerEntry
{
    public ProfilingContext? Context { get; }
    public string TypeName { get; }
    public string FullName { get; }
    public double AverageMilliseconds { get; }
    public double TotalMilliseconds { get; }
}
