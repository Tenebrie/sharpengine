using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Engine.Core.Logging;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    private Backstage? _backstage;
    public Backstage Backstage
    {
        get
        {
            if (_backstage != null)
                return _backstage;
            if (this is Backstage backstage)
                return backstage;
            throw new NullReferenceException("Atom is not registered in a Backstage.");
        }
        internal set => _backstage = value;
    }

    public Atom? Parent { get; internal set; }
    public List<Atom> Children { get; } = [];
    
    // Whether the atom itself is ready (excluding children).
    [UsedImplicitly] private bool _isInitialized = false;
    // Whether the atom and all its children are ready.
    [UsedImplicitly] private bool _isReady = false;
    
    internal void Initialize()
    {
        if (_isInitialized)
            throw new InvalidOperationException("Atom is already initialized.");
        // Initialize the atom internals first. Children will be created, but not adopted until later.
        InitializeReflection();
        InitializeSignals();
        InitializeGroups();
        InitializeComponents();
        InitializeLifecycle();
        
        // Timers after lifecycle
        InitializeTimers();
        InitializeInput();

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
