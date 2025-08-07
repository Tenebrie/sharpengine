using System.Diagnostics;
using Engine.Core.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Engine.Core.Profiling;

public enum ProfilingContext
{
    Unknown,
    PhysicsUpdate,
    OnCreateCallback,
    OnReadyCallback,
    OnUpdateCallback,
    OnDestroyCallback,
}

public class Profiler
{
    private readonly Dictionary<Type, Dictionary<string, ProfilerEntry>> _methodEntries = new();
    private readonly Dictionary<Type, Dictionary<ProfilingContext, ProfilerEntry>> _lifecycleEntries = new();

    private static readonly DefaultObjectPoolProvider PoolProvider = new();
    private static readonly ObjectPool<ProfilingStopwatch> Pool = PoolProvider.Create<ProfilingStopwatch>();
    private static Profiler Instance { get; } = new();
    
    public static ProfilingStopwatch Start()
    {
        var stopwatch = Pool.Get();
        // var stopwatch = new ProfilingStopwatch();
        stopwatch.Start();
        return stopwatch;
    }
    
    public static void GenerateReport()
    {
        return;
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
        return;
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
        Pool.Return(stopwatch);
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
        Pool.Return(stopwatch);
    }
}

public sealed class ProfilingStopwatch : IDisposable
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

    public void Dispose()
    {
        Stopwatch.Stop();
    }
}

internal struct ProfilerEntry
{
    private int _ptr = 0;
    private readonly long[] _durations = new long[100000];

    public ProfilerEntry()
    {
    }

    internal void RecordDuration(long duration)
    {
        _durations[_ptr++ % 50000] = duration;
    }

    internal double Average() => _durations.Average() / 1000.0;
    internal double Total() => _durations.Sum() / 1000.0;
    internal double Count() => _ptr;
}
