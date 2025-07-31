// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.Reflection;

namespace Engine.Core.Signals;

class IAtom
{
    public bool IsValid { get; set; }
}

internal sealed class WeakAction<T>(T original) where T : Delegate
{
    private readonly WeakReference _targetRef = new(original.Target);
    private readonly MethodInfo    _method = original.Method;

    public bool TryInvoke(params object?[] args)
    {
        var target = _targetRef.Target;
        if (target is null)
            return false;
        _method.Invoke(target, args);
        return true;
    }
}

public class BaseSignal<TDelegate> where TDelegate : Delegate
{
    private TDelegate? _subs;
    public TDelegate? Snapshot() => Volatile.Read(ref _subs);

    public void Connect(TDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        TDelegate? prev, next;
        do
        {
            prev = _subs;
            next = (TDelegate?)Delegate.Combine(prev, handler);
        } while (Interlocked.CompareExchange(ref _subs, next, prev) != prev);
    }

    public void Disconnect(TDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        TDelegate? prev, next;
        do
        {
            prev = _subs;
            next = (TDelegate?)Delegate.Remove(prev, handler);
        } while (Interlocked.CompareExchange(ref _subs, next, prev) != prev);
    }
}

public class Signal
{
    private readonly BaseSignal<Action> _baseSignal = new();
    public void Emit() => _baseSignal.Snapshot()?.Invoke();
    public void Connect(Action action) => _baseSignal.Connect(action);
    public void Disconnect(Action action) => _baseSignal.Disconnect(action);
    public static Signal operator +(Signal signal, Action action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal operator -(Signal signal, Action action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1>
{
    private readonly BaseSignal<Action<T1>> _baseSignal = new();
    public void Emit(T1 v1) => _baseSignal.Snapshot()?.Invoke(v1);
    public void Connect(Action<T1> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1> action) => _baseSignal.Disconnect(action);
    public static Signal<T1> operator +(Signal<T1> signal, Action<T1> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1> operator -(Signal<T1> signal, Action<T1> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1, T2>
{
    private readonly BaseSignal<Action<T1, T2>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2) => _baseSignal.Snapshot()?.Invoke(v1, v2);
    public void Connect(Action<T1, T2> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1, T2> action) => _baseSignal.Disconnect(action);
    public static Signal<T1, T2> operator +(Signal<T1, T2> signal, Action<T1, T2> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1, T2> operator -(Signal<T1, T2> signal, Action<T1, T2> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1, T2, T3>
{
    private readonly BaseSignal<Action<T1, T2, T3>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3);
    public void Connect(Action<T1, T2, T3> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1, T2, T3> action) => _baseSignal.Disconnect(action);
    public static Signal<T1, T2, T3> operator +(Signal<T1, T2, T3> signal, Action<T1, T2, T3> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1, T2, T3> operator -(Signal<T1, T2, T3> signal, Action<T1, T2, T3> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1, T2, T3, T4>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4);
    public void Connect(Action<T1, T2, T3, T4> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1, T2, T3, T4> action) => _baseSignal.Disconnect(action);
    public static Signal<T1, T2, T3, T4> operator +(Signal<T1, T2, T3, T4> signal, Action<T1, T2, T3, T4> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1, T2, T3, T4> operator -(Signal<T1, T2, T3, T4> signal, Action<T1, T2, T3, T4> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1, T2, T3, T4, T5>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4, T5>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5);
    public void Connect(Action<T1, T2, T3, T4, T5> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1, T2, T3, T4, T5> action) => _baseSignal.Disconnect(action);
    public static Signal<T1, T2, T3, T4, T5> operator +(Signal<T1, T2, T3, T4, T5> signal,
        Action<T1, T2, T3, T4, T5> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1, T2, T3, T4, T5> operator -(Signal<T1, T2, T3, T4, T5> signal,
        Action<T1, T2, T3, T4, T5> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}

public class Signal<T1, T2, T3, T4, T5, T6>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4, T5, T6>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5, v6);
    public void Connect(Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Connect(action);
    public void Disconnect(Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Disconnect(action);
    public static Signal<T1, T2, T3, T4, T5, T6> operator +(Signal<T1, T2, T3, T4, T5, T6> signal,
        Action<T1, T2, T3, T4, T5, T6> action)
    {
        signal.Connect(action);
        return signal;
    }
    public static Signal<T1, T2, T3, T4, T5, T6> operator -(Signal<T1, T2, T3, T4, T5, T6> signal,
        Action<T1, T2, T3, T4, T5, T6> action)
    {
        signal.Disconnect(action);
        return signal;
    }
}
