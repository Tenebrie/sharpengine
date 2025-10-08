using System.Buffers;
using System.Reflection;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Enum;
using Engine.Core.Modules;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private Action? OnCreateCallback { get; set; }
    private Action? OnReadyCallback { get; set; }

    private bool IsTicking => HasOnUpdateCallbacks || HasOnTimerCallbacks;
    public double TimeScale { get; set; } = 1.0;
    private bool HasOnUpdateCallbacks { get; set; }
    private Action<double>? OnUpdateCallback { get; set; }

    public Action? OnDestroyCallback { get; set; }

    private Dictionary<EngineModule, Action?> OnModuleReloadCallback { get; } = new();
    private Action? OnGameplayContextChangeCallback { get; set; }
    
    private void InitializeLifecycle()
    {
        var data = ReflectionDataCache.GetValueOrDefault(GetType());
        foreach (var reflection in data.OnCreateMethods)
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            OnCreateCallback += (Action)action;
        }

        foreach (var reflection in data.OnReadyMethods)
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            OnReadyCallback += (Action)action;
        }

        foreach (var reflection in data.OnUpdateMethods.Where(reflection => reflection.ParameterCount == 0).ToList())
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            OnUpdateCallback += DelegateHelpers.AsDoubleCallback((Action)action);
        }

        foreach (var reflection in data.OnUpdateMethods.Where(reflection => reflection.ParameterCount > 0).ToList())
        {
            var action = Delegate.CreateDelegate(typeof(Action<double>), this, reflection.MethodInfo);
            OnUpdateCallback += (Action<double>)action;
        }

        HasOnUpdateCallbacks = data.OnUpdateMethods.Count > 0;

        foreach (var reflection in data.OnDestroyMethods)
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            OnDestroyCallback += (Action)action;
        }

        foreach (var reflection in data.OnModuleReloadMethods)
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            if (OnModuleReloadCallback.ContainsKey(reflection.Attribute.Module))
                OnModuleReloadCallback[reflection.Attribute.Module] += (Action)action;
            else
                OnModuleReloadCallback[reflection.Attribute.Module] = (Action)action;
        }
        
        foreach (var reflection in data.OnGameplayContextChangeMethods)
        {
            var action = Delegate.CreateDelegate(typeof(Action), this, reflection.MethodInfo);
            OnGameplayContextChangeCallback += (Action)action;
        }
    }

    private static readonly ArrayPool<Atom> AtomPool = ArrayPool<Atom>.Create();
    protected void ProcessLogicFrame(double deltaTime)
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

    protected void ProcessModuleReload(EngineModule module)
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

    protected void ProcessGameplayContextChanged(GameplayContext context)
    {
        OnGameplayContextChangeCallback?.Invoke();

        var count = Children.Count;
        var buffer = AtomPool.Rent(count);
        try
        {
            for (var i = 0; i < count; i++)
                buffer[i] = Children[i];

            for (var i = 0; i < count; i++)
                buffer[i].ProcessGameplayContextChanged(context);
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

        Parent.RemoveChild(this);
        Backstage = null!;
    }

    public bool IsBeingDestroyed { get; private set; }
    private bool IsFinalized { get; set; }
    public void QueueFree()
    {
        if (IsBeingDestroyed)
            return;
        IsBeingDestroyed = true;
        Parent?.RemoveChild(this);
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
