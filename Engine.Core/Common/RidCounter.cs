namespace Engine.Core.Common;

public static class RidCounter
{
    private static long _current = 0;

    public static long Next()
    {
        return Interlocked.Increment(ref _current);
    }
}