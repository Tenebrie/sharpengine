using System.Collections.Immutable;
using System.Reflection;
using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom : ISignalSubscriber
{
    private ImmutableArray<SignalSubscription> _signalSubscriptions = [];
    public ref ImmutableArray<SignalSubscription> SignalSubscriptions => ref _signalSubscriptions;
    
    private void InitializeSignals()
    {
        var fields = GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var componentFields = fields
            .Where(method => !method.IsStatic && method.GetCustomAttributes<SignalAttribute>().Any())
            .ToList();
        
        foreach (var field in componentFields.Where(field => field.GetValue(this) == null))
        {
            var signal = CreateSignalInstance(field.FieldType);
            field.SetValue(this, signal);
        }
    }

    private static Signal CreateSignalInstance(Type type)
    {
        if (type is not { IsClass: true })
            throw new Exception("Type " + type.Name + " is not a valid signal type (signals must be classes).");
        
        if (type is not { IsAbstract: false })
            throw new Exception("Type " + type.Name + " is not a valid signal type (signals must not be abstract).");
        
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new Exception("Type " + type.Name + " is not a valid signal type (signals must have a parameterless constructor).");
        
        var newInstance = Activator.CreateInstance(type);
        if (newInstance is not Signal signal)
            throw new Exception("Type " + type.Name + " is not a valid signal type (signals must inherit from Signal).");
        
        return signal;
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
