namespace Engine.Core.Communication.Multithreading;

public static class GlobalLock
{
    public static Lock ImmediateContext { get; set; } = new();
    
    public class Lock
    {
        public readonly SemaphoreSlim RenderGate = new(1, 1);
        public readonly object LockObject = new();
        public LockToken Acquire()
        {
            return new LockToken(LockObject);
        }
    }
    public class LockToken : IDisposable
    {
        private readonly object _lockObject;
        public LockToken(object lockObject)
        {
            Monitor.Enter(lockObject);
            _lockObject = lockObject;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Monitor.Exit(_lockObject);
        }
    }
}