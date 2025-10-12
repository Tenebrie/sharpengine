namespace Engine.Core.Common;

public static class FrameCounter
{
    private static long _current = 0;
    public static long Current => Interlocked.Read(ref _current);

    public static void Increment()
    {
        Interlocked.Increment(ref _current);
    }
}