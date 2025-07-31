using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine.Core.Attributes;
using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Services;
using InputService = Engine.Worlds.Services.InputService;

namespace Engine.Worlds.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public Action? OnInitCallback { get; set; }
    
    public bool IsTicking => HasOnUpdateCallbacks || HasOnTimerCallbacks;
    public double TimeScale { get; set; } = 1.0;
    private bool HasOnUpdateCallbacks { get; set; }
    public Action<double>? OnUpdateCallback { get; set; }
    
    public Action? OnDestroyCallback { get; set; }
    
    public Action? OnGameplayContextChangedCallback { get; set; }
    
    private void InitializeLifecycle()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var initMethods = methods.Where(method => method.GetCustomAttribute<OnInitAttribute>() != null).ToList();
        foreach (var action in initMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnInitCallback += (Action)action;
        }

        var updateMethods = methods.Where(method => method.GetCustomAttribute<OnUpdateAttribute>() != null).ToArray();
        var simpleUpdateMethods = updateMethods.Where(method => method.GetParameters().Length == 0).ToList();
        var properUpdateMethods = updateMethods.Where(method => method.GetParameters().Length > 0).ToList();
        foreach (var action in simpleUpdateMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnUpdateCallback += DelegateHelpers.AsDoubleCallback((Action)action);
        }
        foreach (var action in properUpdateMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action<double>), this, methodInfo)))
        {
            OnUpdateCallback += (Action<double>)action;
        }
        HasOnUpdateCallbacks = updateMethods.Length != 0;

        var destroyMethods = methods.Where(method => method.GetCustomAttribute<OnDestroyAttribute>() != null).ToList();
        foreach (var action in destroyMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnDestroyCallback += (Action)action;
        }
        
        var gameplayContextMethods = methods.Where(method => method.GetCustomAttribute<OnGameplayContextChangedAttribute>() != null).ToList();
        foreach (var action in gameplayContextMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnGameplayContextChangedCallback += (Action)action;
        }
    }
    
    protected internal void ProcessLogicFrame(double deltaTime)
    {
        var localDelta = deltaTime * TimeScale;
        if (IsTicking)
        {
            var selfPf = Profiler.Start();
            OnUpdateCallback?.Invoke(localDelta);

            selfPf.StopAndReport(GetType(), ProfilingContext.OnUpdateCallback);
        }


        var count = Children.Count;
        var buffer = ArrayPool<Atom>.Shared.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessLogicFrame(localDelta);
        }
        finally
        {
            ArrayPool<Atom>.Shared.Return(buffer, clearArray: false);
        }
    }
    
    protected internal void ProcessGameplayContextChanged()
    {
        OnGameplayContextChangedCallback?.Invoke();
        
        var count = Children.Count;
        var buffer = ArrayPool<Atom>.Shared.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessGameplayContextChanged();
        }
        finally
        {
            ArrayPool<Atom>.Shared.Return(buffer, clearArray: false);
        }
    }
    
    public void FreeImmediately()
    {
        IsFinalized = true;
        var childrenCount = Children.Count;
        while (childrenCount > 0)
        {
            Children[0].FreeImmediately();
            if (Children.Count >= childrenCount)
                throw new InvalidOperationException("Child count did not decrease after FreeImmediately call.");
            childrenCount = Children.Count;
        }
        
        GetService<InputService>().ClearSubscriptions(this);
        
        OnDestroyCallback?.Invoke();
        if (Parent == null) return;
        
        Parent.Children.Remove(this);
        Backstage = null!;
    }

    public bool IsBeingDestroyed { get; internal set; }
    public bool IsFinalized { get; internal set; }
    public void QueueFree()
    {
        if (IsBeingDestroyed)
            return;
        IsBeingDestroyed = true;
        GetService<ReaperService>().Condemn(this);
    }
}

internal static class DelegateHelpers
{
    // This static method matches (Action, double) → void.
    // When we bind the first argument to our Action instance,
    // it “drops” the double for us.
    private static void InvokeDropFirst(Action target, double _)
        => target();

    // Wrap your Action so you get an Action<double> back.
    internal static Action<double> AsDoubleCallback(Action action)
    {
        // cache this MethodInfo somewhere if you do it often
        var mi = typeof(DelegateHelpers)
            .GetMethod(nameof(InvokeDropFirst),
                BindingFlags.NonPublic | BindingFlags.Static);

        return (Action<double>)Delegate.CreateDelegate(
            typeof(Action<double>),
            action, // bound to the 'target' param of InvokeDropFirst
            mi!
        );
    }
}
