namespace Engine.Module.Physics.WorkerThreads;

public class WorkerPool
{
    public const int MaxWorkerThreads = 8;
    private readonly List<WorkerPoolMember> _workerThreads = new(MaxWorkerThreads);
    
    public void Initialize()
    {
        for (var i = 0; i < MaxWorkerThreads; i++)
        {
            _workerThreads.Add(new WorkerPoolMember(i));
        }
    }
    
    public WorkerPoolMember GetWorker(int index)
    {
        if (index < 0 || index >= _workerThreads.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "Worker index is out of range.");
        }
        return _workerThreads[index];
    }
    
    public void Shutdown()
    {
        for (var i = 0; i < MaxWorkerThreads; i++)
        {
            _workerThreads[i].ShutdownAndWait();
        }
    }
}