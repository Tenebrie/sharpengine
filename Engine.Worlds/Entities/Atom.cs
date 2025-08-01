using System.Diagnostics.CodeAnalysis;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using JetBrains.Annotations;

namespace Engine.Worlds.Entities;

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
        // Initialize the atom internals first. Children will be created, but not adopted until later.
        InitializeSignals();
        InitializeComponents();
        InitializeLifecycle();
        // Timers after lifecycle
        InitializeTimers();
        InitializeInput();
        
        _isInitialized = true;
        
        // Adopt and init children.
        InitializeChildren();

        if (OnInitCallback != null)
        {
            var stopwatch = Profiler.Start();
            OnInitCallback.Invoke();
            stopwatch.StopAndReport(GetType(), ProfilingContext.OnInitCallback);
        }

        _isReady = true;
    }

    public static void InitializeStatic(Type type)
    {
        
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
