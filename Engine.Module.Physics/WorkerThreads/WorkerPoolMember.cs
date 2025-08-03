using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Module.Physics.WorkerThreads;

public class WorkerPoolMember
{
    private double _deltaTime;
    public readonly List<PhysicsTaskDispatcher.TaskDefinition> TaskQueue = [];

    private bool _isRunning = true;
    private readonly ManualResetEventSlim _waitingForTasksEvent = new(false);
    private readonly ManualResetEventSlim _tasksDoneEvent = new(false);

    public WorkerPoolMember(int id)
    {
        var thread = new Thread(WorkerLoop)
        {
            Name = $"PhysicsWorker-{id}",
            IsBackground = true
        };
        thread.Start();
    }

    public void Poke(double deltaTime)
    {
        _deltaTime = deltaTime;
        _waitingForTasksEvent.Set();
    }

    public void WaitUntilDone()
    {
        _tasksDoneEvent.Wait();
        _tasksDoneEvent.Reset();
    }
    
    public void ShutdownAndWait()
    {
        _isRunning = false;
        _waitingForTasksEvent.Set();
        _tasksDoneEvent.Wait();
        _waitingForTasksEvent.Dispose();
        _tasksDoneEvent.Dispose();
    }
    
    private void WorkerLoop()
    {
        while (_isRunning)
        {
            _waitingForTasksEvent.Wait();
            _waitingForTasksEvent.Reset();
            foreach (var item in TaskQueue)
            {
                for (var index = item.StartIndex; index < item.StartIndex + item.Count; index++)
                {
                    try
                    {
                        ProcessTask(item.Type, ref item.AtomHandles[index]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{Thread.CurrentThread.Name}] Error: {ex}");
                    }
                }
            }

            TaskQueue.Clear();
            _tasksDoneEvent.Set();
        }
    }
    
    private void ProcessTask(PhysicsTaskDispatcher.PhysicsTaskType type, ref AtomHandle atomHandle)
    {
        switch (type)
        {
            case PhysicsTaskDispatcher.PhysicsTaskType.CollectData:
                CollectData(ref atomHandle);
                break;
            case PhysicsTaskDispatcher.PhysicsTaskType.InitialMove:
                ProcessAtomMovement(atomHandle);
                break;
            case PhysicsTaskDispatcher.PhysicsTaskType.FlushTransform:
                FlushTransform(atomHandle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    private static void CollectData(ref AtomHandle handle)
    {
        handle.WorldTransform = handle.Parent.WorldTransform;
        handle.Velocity = handle.Component.Velocity;
        handle.SphereColliders = handle.Component.GetSphereColliders();
    }

    private void ProcessAtomMovement(AtomHandle handle)
    {
        if (handle.WorldTransform.Position.Y > 0)
        {
            handle.Velocity.Y -= 35.0 * _deltaTime;
        }
        if (handle.Velocity.LengthSquared <= 0.0001)
            return;
        
        handle.WorldTransform.TranslateGlobal(handle.Velocity * _deltaTime);
        if (handle.WorldTransform.Position.Y > 0)
            return;
        
        handle.WorldTransform.Position = new Vector3(handle.WorldTransform.Position.X, 0, handle.WorldTransform.Position.Z);
        handle.Velocity.Y = 0;
    }

    private static void FlushTransform(AtomHandle handle)
    {
        var localTransform = handle.WorldTransform;
        if (handle.Parent.Parent is Spatial higherLevelParent)
        {
            higherLevelParent.WorldTransform.Inverse.Multiply(localTransform, ref handle.Parent.TransformReference);
        }
        RevalidateWorldTransform(handle.Parent);
    }
    
    public static void RevalidateWorldTransform(Spatial atom)
    {
        SpatialExternals.InvalidateWorldTransform(atom);
        _ = atom.WorldTransform;
        foreach (var child in atom.Children)
        {
            if (child is not Spatial spatial)
                continue;
            RevalidateWorldTransform(spatial);
        }
    }
}