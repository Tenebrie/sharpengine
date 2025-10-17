using System.Diagnostics.CodeAnalysis;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.Exceptions;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private double TimeScale { get; set; } = 1.0;
    public double GlobalTimeScale => Parent?.GlobalTimeScale * TimeScale ?? TimeScale;

    private List<TemporalModifierToken>? _incomingTemporalModifiers = null;
    private List<TemporalModifierToken>? _outgoingTemporalModifiers = null;
    private void RegisterIncomingTemporalModifier(TemporalModifierToken token)
    {
        _incomingTemporalModifiers ??= [];
        _incomingTemporalModifiers.Add(token);
        UpdateTimeScaleFromModifiers();
    }
    private void ReleaseIncomingTemporalModifier(TemporalModifierToken token)
    {
        _incomingTemporalModifiers?.Remove(token);
        UpdateTimeScaleFromModifiers();
    }
    private void UpdateTimeScaleFromModifiers()
    {
        if (_incomingTemporalModifiers == null || _incomingTemporalModifiers.Count == 0)
        {
            TimeScale = 1.0;
            return;
        }

        var combinedModifier = 1.0;
        foreach (var modifier in _incomingTemporalModifiers)
        {
            combinedModifier *= modifier.Modifier;
        }
        TimeScale = combinedModifier;
    }
    
    private void RegisterOutgoingTemporalModifier(TemporalModifierToken token)
    {
        _outgoingTemporalModifiers ??= [];
        _outgoingTemporalModifiers.Add(token);
    }

    private void ReleaseOutgoingTemporalModifier(TemporalModifierToken token)
    {
        _outgoingTemporalModifiers?.Remove(token);
    }
    
    public readonly struct TemporalModifierToken(Atom pauseSource, Atom pauseTarget) : IDisposable, IEquatable<TemporalModifierToken>
    {
        private Guid Id { get; } = Guid.NewGuid();
        private Atom PauseSource { get; } = pauseSource;
        private Atom PauseTarget { get; } = pauseTarget;
        public required double Modifier { get; init; }

        public void Dispose()
        {
            if (IsValid(PauseTarget))
                PauseTarget.ReleaseIncomingTemporalModifier(this);
            if (IsValid(PauseSource))
                PauseSource.ReleaseOutgoingTemporalModifier(this);
        }
        
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is TemporalModifierToken other)
                return Id.Equals(other.Id);
            return false;
        }
        public bool Equals(TemporalModifierToken other)
        {
            return Id.Equals(other.Id);
        }
        public override int GetHashCode() => Id.GetHashCode();
        public static bool operator ==(TemporalModifierToken left, TemporalModifierToken right) => left.Equals(right);
        public static bool operator !=(TemporalModifierToken left, TemporalModifierToken right) => !(left == right);
    }
    
    /**
     * Adds a timescale modifier to this Atom. The modifier can be released by disposing the returned TemporalModifierToken.
     * Multiple modifiers can be active at the same time, and their effects will be multiplied together.
     */
    private TemporalModifierToken SetTimeScale(Atom source, double timeMultiplier)
    {
        var token = new TemporalModifierToken(source, this) { Modifier = timeMultiplier };
        RegisterIncomingTemporalModifier(token);
        source.RegisterOutgoingTemporalModifier(token);
        return token;
    }

    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void Pause() => throw new WeaverDidNotRunException();
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public void Unpause() => throw new WeaverDidNotRunException();

    [PublicAPI]
    public void PauseBy(Atom source)
    {
        var token = SetTimeScale(source,0.0);
        source._outgoingTemporalModifiers ??= [];
        source._outgoingTemporalModifiers.Add(token);
    }

    [PublicAPI]
    public void UnpauseBy(Atom source)
    {
        while (source._outgoingTemporalModifiers is { Count: > 0 })
            source._outgoingTemporalModifiers.First().Dispose();
    }

    [OnDestroy]
    protected void OnDestroy() 
    {
        while (_incomingTemporalModifiers is { Count: > 0 })
            _incomingTemporalModifiers.First().Dispose();
        
        while (_outgoingTemporalModifiers is { Count: > 0 })
            _outgoingTemporalModifiers.First().Dispose();
    }
}