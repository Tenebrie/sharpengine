using System.Collections.Immutable;
using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom : ISignalSubscriber
{
    private ImmutableArray<IDisposable> _signalSubscriptions = [];
    public ref ImmutableArray<IDisposable> SignalSubscriptions => ref _signalSubscriptions;
    
    private void InitializeSignals()
    {
        var data = ReflectionDataCache.GetValueOrDefault(GetType());

        foreach (var reflection in data.SignalFields)
        {
            if (reflection.GetValue(this) != null)
                continue;
            var signal = reflection.Factory();
            reflection.SetValue(this, signal);
        }
    }

    [OnDestroy]
    protected void OnClearSubscriptions()
    {
        // Defensive copy
        var signalSubscriptions = SignalSubscriptions;
        foreach (var signalSubscription in signalSubscriptions)
            signalSubscription.Dispose();
    }
}
