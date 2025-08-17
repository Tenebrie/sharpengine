// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Engine.Core.Communication.Signals;

public interface ISignalSubscriber
{
    ref ImmutableArray<IDisposable> SignalSubscriptions { get; }
}

public interface ISignalSubscription : IDisposable
{
}

internal sealed class SignalSubscription<TDelegate>(
    BaseSignal<TDelegate> owner,
    TDelegate handler,
    BaseSignal<TDelegate>.Node node)
    : IDisposable
    where TDelegate : Delegate
{
    private int _disposed;

    public TDelegate Handler { get; } = handler;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        owner.DisconnectNode(node);
    }
}

public sealed class BaseSignal<TDelegate> where TDelegate : Delegate
{
    internal sealed class Node
    {
        public TDelegate Handler = null!;
        public volatile bool Alive = true;
        public Node? Next;
    }

    private readonly Lock _gate = new();

    private Node? _head;
    private int _liveCount;
    private int _deadCount;
    private int _version;

    // Cached combined delegate + version
    private TDelegate? _cached;
    private int _cachedVersion;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDelegate? Snapshot()
    {
        // Fast path: read cached without locking
        var cached = Volatile.Read(ref _cached);
        if (cached != null && Volatile.Read(ref _cachedVersion) == Volatile.Read(ref _version))
            return cached;

        lock (_gate)
        {
            if (_cached != null && _cachedVersion == _version)
                return _cached;

            if (_liveCount == 0)
            {
                _cached = null;
                _cachedVersion = _version;
                return null;
            }

            // Rebuild multicast once per version change
            var list = new List<Delegate>(_liveCount);
            for (var p = _head; p != null; p = p.Next)
                if (p.Alive) list.Add(p.Handler);

            _cached = (TDelegate?)Delegate.Combine(list.ToArray());
            _cachedVersion = _version;

            // Optional: compact when >50% tombstones to keep traversal cheap
            if (_deadCount > _liveCount)
                Compact_NoThrow(); // under lock

            return _cached;
        }
    }

    public int Count => Volatile.Read(ref _liveCount);

    public void Connect(ISignalSubscriber sub, TDelegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        Node n;
        lock (_gate)
        {
            n = new Node { Handler = handler, Next = _head };
            _head = n;
            _liveCount++;
            _version++;
            _cached = null; // invalidate
        }

        var subscription = new SignalSubscription<TDelegate>(this, handler, n);
        ImmutableInterlocked.Update(ref sub.SignalSubscriptions, arr => arr.Add(subscription));
    }

    // Public API: still supports handler-based disconnects
    public void Disconnect(Delegate handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_gate)
        {
            for (var p = _head; p != null; p = p.Next)
            {
                if (p.Alive && DelegateEquals(p.Handler, handler))
                {
                    p.Alive = false;
                    _liveCount--;
                    _deadCount++;
                    _version++;
                    _cached = null; // invalidate
                    break; // remove only one occurrence (like Delegate.Remove)
                }
            }
        }
    }

    // Fast path used by SignalSubscription
    internal void DisconnectNode(Node n)
    {
        // Avoid lock if already dead
        if (!n.Alive) return;

        lock (_gate)
        {
            if (!n.Alive) return;
            n.Alive = false;
            _liveCount--;
            _deadCount++;
            _version++;
            _cached = null;
        }
    }

    private static bool DelegateEquals(TDelegate a, Delegate b) => a.Equals(b);

    private void Compact_NoThrow()
    {
        Node? newHead = null;
        Node? tail = null;
        for (var p = _head; p != null; p = p.Next)
        {
            if (!p.Alive) continue;
            if (newHead == null)
                newHead = tail = p;
            else
                tail = (tail!.Next = p);
        }
        if (tail != null) tail.Next = null;
        _head = newHead;
        _deadCount = 0;
        // _liveCount unchanged
    }
}

public class Signal
{
    private readonly BaseSignal<Action> _baseSignal = new();
    public void Emit() => _baseSignal.Snapshot()?.Invoke();
    public void Connect(ISignalSubscriber sub, Action action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action action) => _baseSignal.Disconnect(action);
}

public class Signal<T1>
{
    private readonly BaseSignal<Action<T1>> _baseSignal = new();
    public void Emit(T1 v1) => _baseSignal.Snapshot()?.Invoke(v1);
    public void Connect(ISignalSubscriber sub, Action<T1> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1> action) => _baseSignal.Disconnect(action);
}

public class Signal<T1, T2>
{
    private readonly BaseSignal<Action<T1, T2>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2) => _baseSignal.Snapshot()?.Invoke(v1, v2);
    public void Connect(ISignalSubscriber sub, Action<T1, T2> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1, T2> action) => _baseSignal.Disconnect(action);
}

public class Signal<T1, T2, T3>
{
    private readonly BaseSignal<Action<T1, T2, T3>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3);
    public void Connect(ISignalSubscriber sub, Action<T1, T2, T3> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1, T2, T3> action) => _baseSignal.Disconnect(action);
}

public class Signal<T1, T2, T3, T4>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4);
    public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1, T2, T3, T4> action) => _baseSignal.Disconnect(action);
}

public class Signal<T1, T2, T3, T4, T5>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4, T5>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5);
    public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4, T5> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1, T2, T3, T4, T5> action) => _baseSignal.Disconnect(action);
}

public class Signal<T1, T2, T3, T4, T5, T6>
{
    private readonly BaseSignal<Action<T1, T2, T3, T4, T5, T6>> _baseSignal = new();
    public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5, v6);
    public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Connect(sub, action);
    public void Disconnect(Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Disconnect(action);
}
