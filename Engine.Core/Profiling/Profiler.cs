using System.Diagnostics;

namespace Engine.Core.Profiling;

public enum ProfilingContext
{
    Unknown,
    OnInitCallback,
    OnUpdateCallback,
    OnDestroyCallback,
}

public class Profiler
{
    private readonly Dictionary<Type, Dictionary<string, ProfilerEntry>> _methodEntries = new();
    private readonly Dictionary<Type, Dictionary<ProfilingContext, ProfilerEntry>> _lifecycleEntries = new();

    private static Profiler Instance { get; } = new();
    
    public static ProfilingStopwatch Start()
    {
        var stopwatch = new ProfilingStopwatch();
        stopwatch.Start();
        return stopwatch;
    }
    
    internal static void ReportByContext(ProfilingStopwatch stopwatch, Type ownerType, ProfilingContext context)
    {
        if (!Instance._lifecycleEntries.TryGetValue(ownerType, out var contextDictionary))
        {
            contextDictionary = new Dictionary<ProfilingContext, ProfilerEntry>();
            Instance._lifecycleEntries[stopwatch.GetType()] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(context, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry();
            contextDictionary[context] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.ElapsedMilliseconds);
        Console.WriteLine(ownerType.Name + " - " + context + ": " + stopwatch.Stopwatch.ElapsedMilliseconds + " ms");
    }
    
    internal static void ReportByMethodName(ProfilingStopwatch stopwatch, Type ownerType, string methodName)
    {
        if (!Instance._methodEntries.TryGetValue(ownerType, out var contextDictionary))
        {
            contextDictionary = new Dictionary<string, ProfilerEntry>();
            Instance._methodEntries[stopwatch.GetType()] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(methodName, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry();
            contextDictionary[methodName] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.ElapsedMilliseconds);
        // Console.WriteLine(ownerType.Name + " - " + methodName + ": " + stopwatch.Stopwatch.ElapsedMilliseconds + " ms");
    }
}

public class ProfilingStopwatch
{
    internal readonly Stopwatch Stopwatch = new();
    internal void Start()
    {
        Stopwatch.Start();
    }
    
    public void StopAndReport(object owner)
    {
        Stopwatch.Stop();
        Profiler.ReportByContext(this, owner.GetType(), ProfilingContext.Unknown);
    }
    public void StopAndReport(Type owner, ProfilingContext context)
    {
        Stopwatch.Stop();
        Profiler.ReportByContext(this, owner, context);
    }
    public void StopAndReportMethod(Type owner, string methodName)
    {
        Stopwatch.Stop();
        Profiler.ReportByMethodName(this, owner, methodName);
    }
}

internal class ProfilerEntry
{
    private readonly List<long> _durations = [];
    
    internal void RecordDuration(long duration)
    {
        _durations.Add(duration);
        if (_durations.Count > 1000)
        {
            _durations.RemoveRange(0, _durations.Count - 1000);
        }
    }

    internal double Average()
    {
        return _durations.Average();
    }
}
