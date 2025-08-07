using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine.Core.Attributes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Modules;
using Engine.Core.Profiling;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public Action? OnCreateCallback { get; set; }
    public Action? OnReadyCallback { get; set; }

    public bool IsTicking => HasOnUpdateCallbacks || HasOnTimerCallbacks;
    public double TimeScale { get; set; } = 1.0;
    private bool HasOnUpdateCallbacks { get; set; }
    public Action<double>? OnUpdateCallback { get; set; }

    public Action? OnDestroyCallback { get; set; }

    public Dictionary<EngineModule, Action?> OnModuleReloadCallback { get; set; } = new();
    public Action? OnGameplayContextChangeCallback { get; set; }

    private void InitializeLifecycle()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var createMethods = methods.Where(method => method.GetCustomAttribute<OnCreateAttribute>() != null).ToList();
        foreach (var action in createMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnCreateCallback += (Action)action;
        }
        var readyMethods = methods.Where(method => method.GetCustomAttribute<OnReadyAttribute>() != null).ToList();
        foreach (var action in readyMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnReadyCallback += (Action)action;
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

        var moduleReloadedMethods = methods.Where(method => method.GetCustomAttribute<OnModuleReloadAttribute>() != null).ToList();
        foreach (var methodInfo in moduleReloadedMethods)
        {
            var attribute = methodInfo.GetCustomAttribute<OnModuleReloadAttribute>()!;
            var action = Delegate.CreateDelegate(typeof(Action), this, methodInfo);
            if (OnModuleReloadCallback.ContainsKey(attribute.Module))
                OnModuleReloadCallback[attribute.Module] += (Action)action;
            else
                OnModuleReloadCallback[attribute.Module] = (Action)action;
        }
        var gameplayContextMethods = methods.Where(method => method.GetCustomAttribute<OnGameplayContextChangeAttribute>() != null).ToList();
        foreach (var action in gameplayContextMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnGameplayContextChangeCallback += (Action)action;
        }
    }

    protected readonly ArrayPool<Atom> AtomPool = ArrayPool<Atom>.Create();
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
        var buffer = AtomPool.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessLogicFrame(localDelta);
        }
        finally
        {
            AtomPool.Return(buffer, clearArray: false);
        }
    }

    protected internal void ProcessModuleReload(EngineModule module)
    {
        var callback = OnModuleReloadCallback.GetValueOrDefault(module);
        callback?.Invoke();

        var count = Children.Count;
        var buffer = AtomPool.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessModuleReload(module);
        }
        finally
        {
            AtomPool.Return(buffer, clearArray: false);
        }
    }

    protected internal void ProcessGameplayContextChanged()
    {
        OnGameplayContextChangeCallback?.Invoke();

        var count = Children.Count;
        var buffer = AtomPool.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessGameplayContextChanged();
        }
        finally
        {
            AtomPool.Return(buffer, clearArray: false);
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

        foreach (var @delegate in OnReadyCallback?.GetInvocationList() ?? [])
        {
            OnReadyCallback -= (Action)@delegate;
        }
        foreach (var @delegate in OnCreateCallback?.GetInvocationList() ?? [])
        {
            OnCreateCallback -= (Action)@delegate;
        }
        foreach (var @delegate in OnUpdateCallback?.GetInvocationList() ?? [])
        {
            OnUpdateCallback -= (Action<double>)@delegate;
        }
        foreach (var kvp in OnModuleReloadCallback)
        {
            foreach (var @delegate in kvp.Value?.GetInvocationList() ?? [])
            {
                OnModuleReloadCallback[kvp.Key] -= (Action)@delegate;
            }
        }  
        OnReadyCallback = null;
        OnCreateCallback = null;
        OnUpdateCallback = null;
        OnGameplayContextChangeCallback = null;
        OnModuleReloadCallback.Clear();

        GetService<InputService>().ClearSubscriptions(this);

        OnDestroyCallback?.Invoke();
        OnDestroyCallback = null;
        
        foreach (var @delegate in OnDestroyCallback?.GetInvocationList() ?? [])
        {
            OnDestroyCallback -= (Action)@delegate;
        }
        
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
    private static void InvokeDropFirst(Action target, double _)
        => target();

    internal static Action<double> AsDoubleCallback(Action action)
    {
        var mi = typeof(DelegateHelpers)
            .GetMethod(nameof(InvokeDropFirst),
                BindingFlags.NonPublic | BindingFlags.Static);

        return (Action<double>)Delegate.CreateDelegate(
            typeof(Action<double>),
            action,
            mi!
        );
    }
}
