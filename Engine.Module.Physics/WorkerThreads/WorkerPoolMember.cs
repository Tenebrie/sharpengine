using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace Engine.Module.Physics.WorkerThreads;

public class WorkerPoolMember
{
    public enum PhysicsTaskType
    {
        CollectData,
        InitialMove,
        CollectCollisionCandidates,
        FlushTransform,
    }
    
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
                        ProcessTask(item.Type, ref item.AtomHandles[index], item.AtomHandles);
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
    
    private void ProcessTask(PhysicsTaskType type, ref AtomHandle atomHandle, AtomHandle[] allParticipants)
    {
        switch (type)
        {
            case PhysicsTaskType.CollectData:
                CollectData(ref atomHandle);
                break;
            case PhysicsTaskType.InitialMove:
                ProcessAtomMovement(ref atomHandle);
                break;
            case PhysicsTaskType.CollectCollisionCandidates:
                CollectCollisionCandidates(ref atomHandle, allParticipants);
                break;
            case PhysicsTaskType.FlushTransform:
                FlushTransform(ref atomHandle);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
    
    private static void CollectData(ref AtomHandle handle)
    {
        Transform.Copy(handle.Parent.WorldTransform, ref handle.WorldTransform);
        handle.Velocity = handle.Component.Velocity;
        handle.SphereColliders = handle.Component.GetSphereColliders();
        if (handle.SphereColliders.Count == 0)
        {
            handle.BoundingSphereRadius = 1.0;
            return;
        }

        // TODO: Take ALL sphere into account, not just the first one.
        handle.BoundingSphereRadius = handle.SphereColliders[0].WorldRadius;
    }

    private void ProcessAtomMovement(ref AtomHandle handle)
    {
        if (handle.WorldTransform.Position.Y > 0)
        {
            handle.Velocity.Y -= 35.0 * _deltaTime;
        }

        if (handle.Velocity.LengthSquared <= 0.0001)
        {
            handle.WorldPosition = handle.WorldTransform.Position;
            return;
        }
        
        handle.WorldTransform.TranslateGlobal(handle.Velocity * _deltaTime);
        handle.WorldPosition = handle.WorldTransform.Position;
        if (handle.WorldTransform.Position.Y > 0)
        {
            handle.WorldPosition = handle.WorldTransform.Position;
            return;
        }
        
        handle.WorldTransform.Position = new Vector3(handle.WorldTransform.Position.X, 0, handle.WorldTransform.Position.Z);
        handle.WorldPosition = handle.WorldTransform.Position;
        handle.Velocity.Y = 0;
    }
    
    private static void CollectCollisionCandidates(ref AtomHandle handle, AtomHandle[] participants)
    {
        handle.CollisionCandidates.Clear();
        foreach (var participant in participants)
        {
            if (participant.Rid == handle.Rid)
                continue;
            var distanceSquared = participant.WorldPosition.DistanceSquaredTo(handle.WorldPosition);
            var radiusSum = handle.BoundingSphereRadius + participant.BoundingSphereRadius;
            var radiusSumSquared = radiusSum * radiusSum;
            var overlapDistance = radiusSumSquared - distanceSquared;
            if (overlapDistance <= 0)
                continue;
            
            handle.CollisionCandidates.Add(new CollisionCandidate
            {
                OtherHandle = participant,
                OverlapDistance = overlapDistance
            });
        }
    }

    private static void FlushTransform(ref AtomHandle handle)
    {
        if (handle.CollisionCandidates.Count > 0) 
            return;
        var localTransform = handle.WorldTransform;
        handle.Component.Velocity = handle.Velocity;
        if (handle.Parent.Parent is Spatial higherLevelParent)
            higherLevelParent.WorldTransform.Inverse.Multiply(localTransform, ref handle.Parent.TransformReference);
        else
            handle.Parent.Transform = localTransform;

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