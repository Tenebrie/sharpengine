using System.Runtime.CompilerServices;
using Engine.Core.Common;
using Engine.Core.Logging;

namespace Engine.Core.DataStructures;

public struct FrameBufferedArray<TCreator, TValue>
{
    public TValue Current => _arrays[FrameCounter.Current % 2];
    public TValue Previous => _arrays[1 - FrameCounter.Current % 2];

    [InlineArray(2)]
    private struct ValuePair<T> { private T _element0; }
    
    // ReSharper disable once FieldCanBeMadeReadOnly.Local (Not in this case)
    private ValuePair<TValue> _arrays = new();

    public FrameBufferedArray(Func<TValue> factory)
    {
        _arrays[0] = factory();
        _arrays[1] = factory();
    }
    public FrameBufferedArray(TCreator creator, Func<Context, TValue> factory)
    {
        var ctx = new Context
        {
            Creator = creator,
            IsCurrent = true,
            IsPrevious = false
        };
        _arrays[FrameCounter.Current % 2] = factory(ctx);
        ctx = new Context
        {
            Creator = creator,
            IsCurrent = false,
            IsPrevious = true
        };
        _arrays[1 - FrameCounter.Current % 2] = factory(ctx);
    }
    
    public delegate void RefAction<in TProps>(ref TValue value, TProps props);
    public void Mutate<TProps>(TProps props, RefAction<TProps> mutation)
    {
        mutation(ref _arrays[FrameCounter.Current % 2], props);
    }
    public void MutateImmediate<TProps>(TProps props, RefAction<TProps> mutation)
    {
        mutation(ref _arrays[FrameCounter.Current % 2], props);
        mutation(ref _arrays[1 - FrameCounter.Current % 2], props);
    }

    public struct Context
    {
        public required TCreator Creator { get; init; }
        public required bool IsCurrent { get; init; }
        public required bool IsPrevious { get; init; }
    }
}