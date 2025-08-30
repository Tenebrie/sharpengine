using System.Diagnostics;
using Microsoft.Extensions.ObjectPool;

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
    RenderingPrepare = 1 << 6,
}

public class Profiler
{
    private int _currentBufferIndex = 0;
    private readonly Dictionary<string, Dictionary<string, ProfilerEntry>>[] _methodEntriesBuffers = [new(), new()];
    private readonly Dictionary<string, Dictionary<ProfilingContext, ProfilerEntry>>[] _lifecycleEntriesBuffers = [new(), new()];

    private Dictionary<string, Dictionary<string, ProfilerEntry>> MethodEntries => _methodEntriesBuffers[_currentBufferIndex];
    private Dictionary<string, Dictionary<ProfilingContext, ProfilerEntry>> LifecycleEntries => _lifecycleEntriesBuffers[_currentBufferIndex];
    private Dictionary<string, Dictionary<string, ProfilerEntry>> LastSecondMethodEntries => _methodEntriesBuffers[1 - _currentBufferIndex];
    private Dictionary<string, Dictionary<ProfilingContext, ProfilerEntry>> LastSecondLifecycleEntries => _lifecycleEntriesBuffers[1 - _currentBufferIndex];

    private static readonly DefaultObjectPoolProvider PoolProvider = new();
    private static readonly ObjectPool<ProfilingStopwatch> Pool = PoolProvider.Create<ProfilingStopwatch>();
    private static Profiler Instance { get; } = new();
    
    public static int CurrentBufferIndex => Instance._currentBufferIndex;
    
    public static ProfilingStopwatch Start()
    {
        var stopwatch = Pool.Get();
        stopwatch.Start();
        return stopwatch;
    }
    
    public static ProfilerEntry[] Query(ProfilingContext context)
    {
        return Instance.LastSecondLifecycleEntries
            .SelectMany(kvp => kvp.Value)
            .Where(kvp => context.HasFlag(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToArray();
    }
    
    public static ProfilerEntry[] QueryWorstOffenders()
    {
        return Instance.LastSecondMethodEntries
            .SelectMany(kvp => kvp.Value)
            .Select(kvp => kvp.Value)
            .OrderByDescending(e => e.AverageMilliseconds())
            .ToArray();
    }
    
    public static void SwapBuffers()
    {
        Instance._currentBufferIndex = 1 - Instance._currentBufferIndex;
        foreach (var instanceMethodEntry in Instance.MethodEntries)
        {
            var outdatedEntries = instanceMethodEntry.Value.Where(v => v.Value.Count() == 0).ToList();
            foreach (var outdatedEntry in outdatedEntries)
                instanceMethodEntry.Value.Remove(outdatedEntry.Key);
            foreach (var profilerEntry in instanceMethodEntry.Value.Values)
                profilerEntry.Clear();
        }
        foreach (var instanceLifecycleEntry in Instance.LifecycleEntries)
        {
            var outdatedEntries = instanceLifecycleEntry.Value.Where(v => v.Value.Count() == 0).ToList();
            foreach (var outdatedEntry in outdatedEntries)
                instanceLifecycleEntry.Value.Remove(outdatedEntry.Key);
            foreach (var profilerEntry in instanceLifecycleEntry.Value.Values)
                profilerEntry.Clear();
        }
    }
    
    internal static void ReportByContext(ProfilingStopwatch stopwatch, Type ownerType, ProfilingContext context)
    {
        if (!Instance.LifecycleEntries.TryGetValue(ownerType.Name, out var contextDictionary))
        {
            contextDictionary = new Dictionary<ProfilingContext, ProfilerEntry>();
            Instance.LifecycleEntries[ownerType.Name] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(context, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry
            {
                TypeName = ownerType.Name,
                MethodName = "Context: " + context
            };
            contextDictionary[context] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.Elapsed.TotalMicroseconds);
        Pool.Return(stopwatch);
    }
    
    internal static void ReportByMethodName(ProfilingStopwatch stopwatch, Type ownerType, string methodName)
    {
        if (!Instance.MethodEntries.TryGetValue(ownerType.Name, out var contextDictionary))
        {
            contextDictionary = new Dictionary<string, ProfilerEntry>();
            Instance.MethodEntries[ownerType.Name] = contextDictionary;
        }
        if (!contextDictionary.TryGetValue(methodName, out var profilerEntry))
        {
            profilerEntry = new ProfilerEntry
            {
                TypeName = ownerType.Name,
                MethodName = methodName
            };
            contextDictionary[methodName] = profilerEntry;
        }
        profilerEntry.RecordDuration(stopwatch.Stopwatch.Elapsed.TotalMicroseconds);
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

public class ProfilerEntry
{
    private int _ptr = 0;
    private readonly double[] _durations = new double[10000];

    internal void RecordDuration(double duration)
    {
        _durations[_ptr++ % 10000] = duration;
    }
    
    internal void Clear()
    {
        _ptr = 0;
        Array.Clear(_durations, 0, _durations.Length);
    }

    public string TypeName { get; internal set; } = string.Empty;
    public string MethodName { get; internal set; } = string.Empty;
    public string FullName => $"{TypeName}.{MethodName}";
    public double AverageMilliseconds() => TotalMilliseconds() / _ptr;
    public double TotalMilliseconds() => _durations.Sum() / 1000.0;
    public double Count() => _ptr;
}
