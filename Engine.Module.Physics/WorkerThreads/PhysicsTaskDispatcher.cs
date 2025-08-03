using Engine.Module.Physics.Utilities;

namespace Engine.Module.Physics.WorkerThreads;

public class PhysicsTaskDispatcher
{
    public enum PhysicsTaskType
    {
        CollectData,
        InitialMove,
        FlushTransform,
    }
    
    public struct TaskDefinition
    {
        public PhysicsTaskType Type;
        public AtomHandle[] AtomHandles;
        public int StartIndex;
        public int Count;
    }
    
    public void Dispatch(WorkerPool workerPool, double deltaTime, PhysicsTaskType taskType, AtomHandle[] atoms)
    {
        var startIndex = 0;
        var threadsPoked = 0;
        var chunkSize = atoms.Length / WorkerPool.MaxWorkerThreads + 1;
        for (var i = 0; i < WorkerPool.MaxWorkerThreads; i++)
        {
            var worker = workerPool.GetWorker(i);
            var count = Math.Min(chunkSize, atoms.Length - startIndex);
            if (count <= 0)
                break;
            worker.TaskQueue.Add(new TaskDefinition
            {
                Type = taskType,
                AtomHandles = atoms,
                StartIndex = startIndex,
                Count = count
            });
            startIndex += count;
            worker.Poke(deltaTime);
            threadsPoked += 1;
        }

        for (var i = 0; i < threadsPoked; i++)
        {
            workerPool.GetWorker(i).WaitUntilDone();
        }
    }
}