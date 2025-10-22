using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities.BuiltIns;

namespace Engine.Testing.Core.Communication;

public class SignalTest
{
    [Fact]
    public void SignalTest_ShouldConnect()
    {
        var signal = new Signal();

        var signalReceivedCount = 0;
        _ = new SignalSubscriber(signal, () => signalReceivedCount += 1);
        signal.Emit();
        Assert.Equal(1, signalReceivedCount);
    }
    
    [Fact]
    public void Signal_ShouldDisconnect()
    {
        var signal = new Signal();

        var subscriber = new SignalSubscriber(signal, () => throw new InvalidOperationException("Signal should not be received."));
        subscriber.Destroy();
        signal.Emit();
    }
    
    [Fact]
    public void Signal_ShouldKeepReferences()
    {
        var signal = new Signal();

        for (var i = 0; i < 10; i++)
        {
            _ = new SignalSubscriber(signal, () => throw new InvalidOperationException("Signal should not be received."));
        }
        signal.Base.ReadStatus(out var liveCount, out var deadCount, out var compactEvents);
        Assert.Equal(10, liveCount);
        Assert.Equal(0, deadCount);
        Assert.Equal(0, compactEvents);
    }
    
    [Fact]
    public void Signal_ShouldRemoveReferencesWhenDestroyedInBatches()
    {
        var signal = new Signal();

        List<SignalSubscriber> subscribers = [];
        for (var i = 0; i < 10; i++)
        {
            subscribers.Add(new SignalSubscriber(signal, () => throw new InvalidOperationException("Signal should not be received.")));
        }
        subscribers.ForEach(s => s.Destroy());
        signal.Base.ReadStatus(out var liveCount, out var deadCount, out var compactEvents);
        Assert.Equal(0, liveCount);
        Assert.Equal(0, deadCount);
        Assert.Equal(3, compactEvents);
    }
    
    [Fact]
    public void Signal_ShouldRemoveReferencesWhenDestroyedOneAfterAnother()
    {
        var signal = new Signal();

        for (var i = 0; i < 10; i++)
        {
            var subscriber = new SignalSubscriber(signal,
                () => throw new InvalidOperationException("Signal should not be received."));
            subscriber.Destroy();
        }
        signal.Base.ReadStatus(out var liveCount, out var deadCount, out var compactEvents);
        Assert.Equal(0, liveCount);
        Assert.Equal(0, deadCount);
        Assert.Equal(10, compactEvents);
    }

    private partial class SignalSubscriber(Signal signal, Action onSignalReceived) : StandaloneBackstage
    {
        [OnReady]
        public void OnReady()
        {
            signal.Connect(this, onSignalReceived);
        }
    }
}