using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine.Worlds.Attributes;

namespace Engine.Worlds.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    private bool HasOnTimerCallbacks { get; set; }
    private List<AtomTimer> Timers { get; } = [];
    
    private void InitializeTimers()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var timerMethods = methods.Where(method => method.GetCustomAttribute<OnTimerAttribute>() != null).ToList();
        if (timerMethods.Count == 0)
            return;
        
        foreach (var methodInfo in timerMethods)
        {
            var timer = new AtomTimer();
            var attribute = methodInfo.GetCustomAttribute<OnTimerAttribute>(inherit: false);
            if (attribute is null or { Frames: < 0, Seconds: double.NaN } or { Frames: > 0, Seconds: not double.NaN })
                throw new InvalidOperationException(
                    $"Method {methodInfo.Name} has invalid OnTimerAttribute.");

            timer.IntervalFrames = attribute.Frames;
            timer.IntervalSeconds = attribute.Seconds;
            if (methodInfo.GetParameters().Length == 0)
            {
                var onTick = (Action)Delegate.CreateDelegate(typeof(Action), this, methodInfo);
                timer.OnTick += DelegateHelpers.AsDoubleCallback(onTick);
            }
            else
            {
                var onTick = (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, methodInfo);
                timer.OnTick += onTick;
            }
            
            Timers.Add(timer);
        }

        HasOnTimerCallbacks = true;
        OnUpdateCallback += OnTick;
    }
    
    private void OnTick(double deltaTime)
    {
        foreach (var timer in Timers)
        {
            if (timer.IntervalFrames > 0)
            {
                timer.FramesRemaining--;
                if (timer.FramesRemaining > 0) continue;
                
                timer.OnTick.Invoke(deltaTime);
                timer.FramesRemaining = timer.IntervalFrames;
            }
            else if (!double.IsNaN(timer.IntervalSeconds))
            {
                timer.SecondsRemaining -= deltaTime;
                if (timer.SecondsRemaining > 0) continue;
                
                timer.OnTick.Invoke(deltaTime);
                timer.SecondsRemaining = timer.IntervalSeconds;
            }
        }
    }

    private class AtomTimer
    {
        internal int FramesRemaining { get; set; } = -1;
        internal int IntervalFrames { get; set; } = -1;
        
        internal double SecondsRemaining { get; set; } = double.NaN;
        internal double IntervalSeconds { get; set; } = double.NaN;
        
        internal Action<double> OnTick = null!;
    }
}