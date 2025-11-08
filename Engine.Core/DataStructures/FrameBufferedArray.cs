using Engine.Core.Common;

namespace Engine.Core.DataStructures;

public class FrameBufferedSingletonArray<T>
{
    private T[] _first;
    private T[] _second;

    public FrameBufferedSingletonArray()
    {
        _first = new T[1];
        _second = new T[1];
    }

    public T[] Produce(T value)
    {
        if (FrameCounter.Current % 2 == 0)
        {
            _first[0] = value;
            return _first;
        }
        _second[0] = value;
        return _second;
    }
}