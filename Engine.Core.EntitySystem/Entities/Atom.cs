using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public Backstage Backstage { get; internal set; } = null!;
    public Atom? Parent { get; internal set; }
    public List<Atom> Children { get; } = [];
    
    // Whether the atom itself is ready (excluding children).
    [UsedImplicitly] private bool _isInitialized = false;
    // Whether the atom and all its children are ready.
    [UsedImplicitly] private bool _isReady = false;
    
    internal void Initialize()
    {
        var totalStopwatch = new Stopwatch();
        totalStopwatch.Start();
        // Initialize the atom internals first. Children will be created, but not adopted until later.
        var stopwatch1 = new Stopwatch(); stopwatch1.Start();
        InitializeReflection();
        stopwatch1.Stop();
        var stopwatch2 = new Stopwatch(); stopwatch2.Start();
        InitializeSignals();
        stopwatch2.Stop();
        var stopwatch3 = new Stopwatch(); stopwatch3.Start();
        InitializeGroups();
        stopwatch3.Stop();
        var stopwatch4 = new Stopwatch(); stopwatch4.Start();
        InitializeComponents();
        stopwatch4.Stop();
        var stopwatch5 = new Stopwatch(); stopwatch5.Start();
        InitializeLifecycle();
        stopwatch5.Stop();
        var stopwatch6 = new Stopwatch(); stopwatch6.Start();
        // Timers after lifecycle
        InitializeTimers();
        stopwatch6.Stop();
        var stopwatch7 = new Stopwatch(); stopwatch7.Start();
        InitializeInput();
        stopwatch7.Stop();
        totalStopwatch.Stop();
        
        var longestStopwatch = new[]
        {
            (0, stopwatch1.Elapsed.TotalMicroseconds),
            (1, stopwatch2.Elapsed.TotalMicroseconds),
            (2, stopwatch3.Elapsed.TotalMicroseconds),
            (3, stopwatch4.Elapsed.TotalMicroseconds),
            (4, stopwatch5.Elapsed.TotalMicroseconds),
            (5, stopwatch6.Elapsed.TotalMicroseconds),
            (6, stopwatch7.Elapsed.TotalMicroseconds)
        }.OrderByDescending(x => x.TotalMicroseconds).First();
        if (totalStopwatch.Elapsed.TotalMicroseconds > 25)
        {
            Console.WriteLine("Longest stopwatch was {0} with {1}us", longestStopwatch.Item1, longestStopwatch.Item2);
            Console.WriteLine("Total initialization time for {0} was {1}us", GetType().Name,
                totalStopwatch.Elapsed.TotalMicroseconds);
        }

        if (OnCreateCallback != null)
        {
            using var stopwatch = Profiler.Start();
            try
            {
                OnCreateCallback.Invoke();
            } catch (Exception e)
            {
                Logger.Error($"Error during OnCreateCallback callback for {GetType().Name}: {e.Message}");
                Console.Error.WriteLine(e);
            }

            stopwatch.StopAndReport(GetType(), ProfilingContext.OnCreateCallback);
        }
        
        _isInitialized = true;
        
        // Adopt and init children.
        InitializeChildren();

        if (OnReadyCallback != null)
        {
            using var stopwatch = Profiler.Start();
            try
            {
                OnReadyCallback?.Invoke();
            } catch (Exception e)
            {
                Logger.Error($"Error during OnReady callback for {GetType().Name}: {e.Message}");
                Console.Error.WriteLine(e);
            }

            stopwatch.StopAndReport(GetType(), ProfilingContext.OnReadyCallback);
        }

        _isReady = true;
    }

    public T AdoptChild<T>(T atom) where T : Atom, new()
    {
        Children.Add(atom);
        atom.Parent = this;
        if (this is Backstage backstage)
            atom.Backstage = backstage;
        else
            atom.Backstage = Backstage;
        if (_isInitialized)
            atom.Initialize();
        return atom;
    }

    public T GetService<T>() where T : Service, new()
    {
        if (Backstage == null)
            throw new InvalidOperationException("Atom is not registered in a Backstage.");
        return Backstage.ServiceRegistry.Get<T>();
    }

    // Services don't need explicit registration, but this alias helps with clarity for services that are working passively.
    public void RegisterService<T>() where T : Service, new() => GetService<T>();
    
    [SuppressMessage("ReSharper", "RedundantAlwaysMatchSubpattern")]
    public static bool IsValid(Atom? atom)
    {
        return atom is { IsBeingDestroyed: false, Backstage: not null };
    }
    
    public static bool IsStale(Atom? atom)
    {
        return atom is { IsFinalized: true };
    }
}
