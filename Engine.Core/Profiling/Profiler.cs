using System.Diagnostics;
using Engine.Core.Logging;
using Microsoft.Extensions.ObjectPool;

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

    private static DefaultObjectPoolProvider _poolProvider = new();
    private static ObjectPool<ProfilingStopwatch> _pool = _poolProvider.Create<ProfilingStopwatch>();
    private static Profiler Instance { get; } = new();
    
    public static ProfilingStopwatch Start()
    {
        var stopwatch = _pool.Get();
        stopwatch.Start();
        return stopwatch;
    }
    
    public static void GenerateReport()
    {
        Logger.Debug("Profiler Report:");
        foreach (var (ownerType, contextDictionary) in Instance._lifecycleEntries)
        {
            Logger.Debug($"Owner Type: {ownerType.Name}");
            foreach (var (context, profilerEntry) in contextDictionary)
            {
                var averageDuration = $"Average: {profilerEntry.Average()} ms";
                var totalDuration = $"Total: {profilerEntry.Total()} ms ({profilerEntry.Total() / 30.0}%)";
                var callCount = $"Calls: {profilerEntry.Count()}";
                Logger.Debug($"  Context: {context}, {averageDuration}, {totalDuration}, {callCount}");
            }
        }
        
        foreach (var (ownerType, methodDictionary) in Instance._methodEntries)
        {
            Logger.Debug($"Owner Type: {ownerType.Name}");
            foreach (var (methodName, profilerEntry) in methodDictionary)
            {
                var averageDuration = $"Average: {profilerEntry.Average()} ms";
                var totalDuration = $"Total: {profilerEntry.Total()} ms ({profilerEntry.Total() / 30.0}%)";
                var callCount = $"Calls: {profilerEntry.Count()}";
                Logger.Debug($"  Method: {methodName}, {averageDuration}, {totalDuration}, {callCount}");
            }
        }
    }
    
    public static void Reset()
    {
        Instance._methodEntries.Clear();
        Instance._lifecycleEntries.Clear();
    }
    
    internal static void ReportByContext(ProfilingStopwatch stopwatch, Type ownerType, ProfilingContext context)
    {
        if (!Instance._lifecycleEntries.TryGetValue(ownerType, out var contextDictionary))
        {
            contextDictionary = new Dictionary<ProfilingContext, ProfilerEntry>();
            Instance._lifecycleEntries[ownerType] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(context, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry();
            contextDictionary[context] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.Elapsed.Microseconds);
        Logger.Debug(ownerType.Name + " - " + context + ": " + stopwatch.Stopwatch.ElapsedMilliseconds + " ms");
        _pool.Return(stopwatch);
    }
    
    internal static void ReportByMethodName(ProfilingStopwatch stopwatch, Type ownerType, string methodName)
    {
        if (!Instance._methodEntries.TryGetValue(ownerType, out var contextDictionary))
        {
            contextDictionary = new Dictionary<string, ProfilerEntry>();
            Instance._methodEntries[ownerType] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(methodName, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry();
            contextDictionary[methodName] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.Elapsed.Microseconds);
        Logger.Debug(ownerType.Name + " - " + methodName + ": " + stopwatch.Stopwatch.ElapsedMilliseconds + " ms");
        _pool.Return(stopwatch);
    }
}

public class ProfilingStopwatch
{
    internal readonly Stopwatch Stopwatch = new();
    internal void Start()
    {
        Stopwatch.Reset();
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
        if (_durations.Count > 10000)
        {
            _durations.RemoveAt(0);
        }
    }

    internal double Average() => _durations.Average() / 1000.0;
    internal double Total() => _durations.Sum() / 1000.0;
    internal double Count() => _durations.Count;
}
