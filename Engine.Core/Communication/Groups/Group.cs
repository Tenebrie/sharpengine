//
// using System.Collections.Immutable;
//
// namespace Engine.Core.Communication.Groups;
//
// // Implemented by Atom
// public interface IGroupSubscriber
// {
//     public ref ImmutableArray<SignalSubscription> SignalSubscriptions { get; }
// }
//
// public interface IBaseSignal
// {
//     public void Connect(ISignalSubscriber sub, Delegate handler);
//     public void Disconnect(Delegate handler);
// }
//
// public class BaseSignal<TDelegate> : IBaseSignal where TDelegate : Delegate
// {
//     private TDelegate? _subs;
//     public TDelegate? Snapshot() => Volatile.Read(ref _subs);
//
//     public void Connect(ISignalSubscriber sub, Delegate handler)
//     {
//         ArgumentNullException.ThrowIfNull(handler);
//
//         TDelegate? prev, next;
//         do
//         {
//             prev = _subs;
//             next = (TDelegate?)Delegate.Combine(prev, handler);
//         } while (Interlocked.CompareExchange(ref _subs, next, prev) != prev);
//
//         var subscription = new SignalSubscription(this, handler);
//         ImmutableInterlocked.Update(ref sub.SignalSubscriptions,
//             arr => arr.Add(subscription));
//     }
//
//     public void Disconnect(Delegate handler)
//     {
//         ArgumentNullException.ThrowIfNull(handler);
//         TDelegate? prev, next;
//         do
//         {
//             prev = _subs;
//             next = (TDelegate?)Delegate.Remove(prev, handler);
//         } while (Interlocked.CompareExchange(ref _subs, next, prev) != prev);
//     }
// }
//
// public sealed class SignalSubscription : IDisposable
// {
//     private IBaseSignal? _signal;
//     private Delegate? _handler;
//     private bool _disposed = false;
//
//     internal SignalSubscription(IBaseSignal signal, Delegate handler)
//     {
//         _signal  = signal;
//         _handler = handler;
//     }
//
//     public void Dispose()
//     {
//         if (_disposed)
//             return;
//         _disposed = true;
//         var signal  = Interlocked.Exchange(ref _signal, null);
//         var handler = Interlocked.Exchange(ref _handler, null);
//         if (signal is not null && handler is not null)
//             signal.Disconnect(handler);
//     }
// }
//
// public class Signal
// {
//     private readonly BaseSignal<Action> _baseSignal = new();
//     
//     public void Emit() => _baseSignal.Snapshot()?.Invoke();
//     public void Connect(ISignalSubscriber sub, Action action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1>
// {
//     private readonly BaseSignal<Action<T1>> _baseSignal = new();
//     public int SubscriberCount => _baseSignal.Snapshot()?.GetInvocationList().Length ?? 0;
//     public void Emit(T1 v1) => _baseSignal.Snapshot()?.Invoke(v1);
//     public void Connect(ISignalSubscriber sub, Action<T1> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1> action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1, T2>
// {
//     private readonly BaseSignal<Action<T1, T2>> _baseSignal = new();
//     public void Emit(T1 v1, T2 v2) => _baseSignal.Snapshot()?.Invoke(v1, v2);
//     public void Connect(ISignalSubscriber sub, Action<T1, T2> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1, T2> action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1, T2, T3>
// {
//     private readonly BaseSignal<Action<T1, T2, T3>> _baseSignal = new();
//     public void Emit(T1 v1, T2 v2, T3 v3) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3);
//     public void Connect(ISignalSubscriber sub, Action<T1, T2, T3> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1, T2, T3> action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1, T2, T3, T4>
// {
//     private readonly BaseSignal<Action<T1, T2, T3, T4>> _baseSignal = new();
//     public void Emit(T1 v1, T2 v2, T3 v3, T4 v4) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4);
//     public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1, T2, T3, T4> action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1, T2, T3, T4, T5>
// {
//     private readonly BaseSignal<Action<T1, T2, T3, T4, T5>> _baseSignal = new();
//     public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5);
//     public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4, T5> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1, T2, T3, T4, T5> action) => _baseSignal.Disconnect(action);
// }
//
// public class Signal<T1, T2, T3, T4, T5, T6>
// {
//     private readonly BaseSignal<Action<T1, T2, T3, T4, T5, T6>> _baseSignal = new();
//     public void Emit(T1 v1, T2 v2, T3 v3, T4 v4, T5 v5, T6 v6) => _baseSignal.Snapshot()?.Invoke(v1, v2, v3, v4, v5, v6);
//     public void Connect(ISignalSubscriber sub, Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Connect(sub, action);
//     public void Disconnect(Action<T1, T2, T3, T4, T5, T6> action) => _baseSignal.Disconnect(action);
// }
