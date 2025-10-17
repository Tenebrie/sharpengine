using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Module.Physics.Utilities;

namespace Engine.Module.Physics.WorkerThreads;

public class WorkerPoolMember
{
    public enum PhysicsTaskType
    {
        CollectData,
        InitialMove,
        CollectCollisionCandidates,
        ResolveCollisions,
        FlushTransform,
    }
    
    private double _deltaTime;
    public readonly List<PhysicsTaskDispatcher.TaskDefinition> TaskQueue = [];

    private readonly Thread _thread;
    private bool _isRunning = true;
    private readonly ManualResetEventSlim _waitingForTasksEvent = new(false);
    private readonly ManualResetEventSlim _tasksDoneEvent = new(false);

    public WorkerPoolMember(int id)
    {
        _thread = new Thread(WorkerLoop)
        {
            Name = $"PhysicsWorker-{id}",
            IsBackground = true
        };
        _thread.Start();
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
        _thread.Join();
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
    
    private void ProcessTask(PhysicsTaskType type, ref AtomHandle atomHandle, AtomList allParticipants)
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
            case PhysicsTaskType.ResolveCollisions:
                ResolveCollisions(ref atomHandle);
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
        handle.LinearVelocity = handle.Component.LinearVelocity;
        handle.AngularVelocity = handle.Component.AngularVelocity;
        handle.SphereColliders = handle.Component.GetSphereColliders();
        handle.TimeScaleFactor = handle.Component.GlobalTimeScale;
        handle.GravityFactor = Convert.ToDouble(handle.Component.GravityEnabled);
        handle.HasColliders = false;
        if (handle.SphereColliders.Count == 0)
            return;

        handle.HasColliders = true;
        // TODO: Take ALL sphere into account, not just the first one.
        handle.BoundingSphereRadius = handle.SphereColliders[0].WorldRadius;
    }

    private void ProcessAtomMovement(ref AtomHandle handle)
    {
        handle.LinearVelocity.Y -= 9.8 * _deltaTime * handle.TimeScaleFactor * handle.GravityFactor;

        if (handle.LinearVelocity.LengthSquared <= 0.0001)
        {
            handle.WorldPosition = handle.WorldTransform.Position;
            return;
        }
        
        handle.WorldTransform.TranslateGlobal(handle.LinearVelocity * _deltaTime * handle.TimeScaleFactor);
        handle.WorldTransform.Rotate(handle.AngularVelocity * _deltaTime * handle.TimeScaleFactor);
        handle.WorldPosition = handle.WorldTransform.Position;
        
        handle.WorldPosition = handle.WorldTransform.Position;
    }
    
    private static void CollectCollisionCandidates(ref AtomHandle handle, AtomList participants)
    {
        handle.CollisionCandidates.Clear();
        if (!handle.HasColliders)
            return;
        foreach (var participant in participants.AsSpan())
        {
            if (participant.Rid == handle.Rid || !participant.HasColliders)
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
    
    private void ResolveCollisions(ref AtomHandle handle)
    {
        // Early-out if nothing to do
        if (handle.CollisionCandidates.Count == 0 || !handle.HasColliders)
            return;

        const double restitution = 0.5;     // 0 = sticky, 1 = perfectly elastic
        const double percent     = 0.5;     // positional correction strength
        const double slop        = 0.005;       // allow tiny penetration before pushing

        var posA = handle.WorldPosition;
        var velA = handle.LinearVelocity;
        var radiusOfA   = handle.BoundingSphereRadius;

        foreach (var c in handle.CollisionCandidates)
        {
            // Each pair appears twice (A→B and B→A). Resolve only when A.Rid < B.Rid
            var other = c.OtherHandle;
            if (handle.Rid > other.Rid || !other.HasColliders)
                continue;

            var posB = other.WorldPosition;
            var velB = other.LinearVelocity;
            var radiusOfB = other.BoundingSphereRadius;

            // Step 1: compute contact info
            var vectorDelta   = posB - posA;
            var dist    = Math.Sqrt(vectorDelta.LengthSquared);
            if (dist == 0)                   // same centre – pick any axis
                vectorDelta = Vector3.UnitY;

            var normal  = vectorDelta / (dist + double.Epsilon);  // safe normal
            var penDepth= (radiusOfA + radiusOfB) - dist;                 // linear penetration

            if (penDepth <= 0)
                continue;   // they separated since candidate pass

            // Step 2: positional correction (split 50/50)
            var correction = normal * percent * Math.Max(penDepth - slop, 0) * 0.5;
            handle.WorldTransform.TranslateGlobal(-correction);
            other.WorldTransform.TranslateGlobal( correction);

            handle.WorldPosition = handle.WorldTransform.Position;
            other.WorldPosition = other.WorldTransform.Position;

            // Step 3: velocity impulse
            var relVel   = velA - velB;
            var velAlongN= relVel.DotProduct(normal);

            if (velAlongN > 0)
                continue;   // bodies are separating already

            var j = -(1 + restitution) * velAlongN; // equal mass => divide by 2 omitted
            var impulse = normal * (j * 0.5);       // 0.5 each (mass = 1)

            velA +=  impulse;
            velB -=  impulse;

            // Write back to handles (structs!)
            handle.LinearVelocity = velA;
            other.LinearVelocity = velB;
        }

        handle.CollisionCandidates.Clear();
    }

    private static void FlushTransform(ref AtomHandle handle)
    {
        var localTransform = handle.WorldTransform;
        handle.Component.LinearVelocity = handle.LinearVelocity;
        handle.Component.AngularVelocity = handle.AngularVelocity;
        if (handle.Parent.Parent is Spatial higherLevelParent)
            higherLevelParent.WorldTransformInverse.Multiply(localTransform, ref handle.Parent.TransformReference);
        else
            handle.Parent.Transform = localTransform;

        RevalidateWorldTransform(handle.Parent);
    }
    
    public static void RevalidateWorldTransform(Spatial atom)
    {
        SpatialExternals.InvalidateWorldTransform(atom);
        _ = atom.WorldTransform;
        _ = atom.WorldTransformInverse;
        foreach (var child in atom.Children)
        {
            if (child is not Spatial spatial)
                continue;
            RevalidateWorldTransform(spatial);
        }
    }
}