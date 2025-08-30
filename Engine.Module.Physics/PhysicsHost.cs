using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Enum;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;
using Engine.Core.Profiling;
using Engine.Module.Physics.Utilities;
using Engine.Module.Physics.WorkerThreads;
using JetBrains.Annotations;

namespace Engine.Module.Physics;

[UsedImplicitly]
public class PhysicsHost : IPhysicsHost
{
    public required IRootHypervisor Hypervisor { get; set; }
    
    private readonly AtomRegistrationHandler _registeredAtoms = new();
    private readonly CacheRevalidationServiceHandler _revalidationServices = new();

    private readonly WorkerPool _workerPool = new();

    public void RegisterService(ICacheRevalidationService service) => _revalidationServices.Add(service);
    public void UnregisterService(ICacheRevalidationService service) => _revalidationServices.Remove(service);

    public void Initialize() => _workerPool.Initialize();
    public void Shutdown() => _workerPool.Shutdown();

    public long Register(ISpatial maskedParent, IPhysicsComponent maskedComponent)
    {
        if (maskedParent is not Spatial parent)
            throw new ArgumentException("Unable to unmask a Spatial"); 
        if (maskedComponent is not PhysicsComponent component)
            throw new ArgumentException("Unable to unmask a PhysicsComponent");
        return _registeredAtoms.Add(parent, component);
    }
    public void Unregister(long rid) => _registeredAtoms.Remove(rid);

    private const double PhysicsStepDuration = 0.0166666666666667;
    private double _leftoverTime = 0.0;
    public void ProcessPhysicsFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        var atoms = _registeredAtoms.AsArray();
        
        var steps = (int)(deltaTime / PhysicsStepDuration);
        _leftoverTime += deltaTime - steps * PhysicsStepDuration;
        
        steps = Math.Min(8, steps);
        if (_leftoverTime >= PhysicsStepDuration)
        {
            _leftoverTime -= PhysicsStepDuration;
            steps += 1;
        }

        _revalidationServices.DisableAll();

        PhysicsTaskDispatcher.Dispatch(_workerPool, PhysicsStepDuration, WorkerPoolMember.PhysicsTaskType.CollectData, atoms);
        for (var i = 0; i < steps; i++)
        {
            PhysicsTaskDispatcher.Dispatch(_workerPool, PhysicsStepDuration, WorkerPoolMember.PhysicsTaskType.InitialMove, atoms);
            PhysicsTaskDispatcher.Dispatch(_workerPool, PhysicsStepDuration, WorkerPoolMember.PhysicsTaskType.CollectCollisionCandidates, atoms);
            PhysicsTaskDispatcher.Dispatch(_workerPool, PhysicsStepDuration, WorkerPoolMember.PhysicsTaskType.ResolveCollisions, atoms);
        }
        PhysicsTaskDispatcher.Dispatch(_workerPool, PhysicsStepDuration, WorkerPoolMember.PhysicsTaskType.FlushTransform, atoms);
        
        _revalidationServices.EnableAll();
        stopwatch.StopAndReport(GetType(), ProfilingContext.PhysicsUpdate);
    }

    public void RevalidateWorldTransform(ISpatial atom)
    {
        if (atom is not Spatial spatial)
            throw new ArgumentException("Unable to unmask a Spatial", nameof(atom));
        WorkerPoolMember.RevalidateWorldTransform(spatial);
    }

    public void NotifyModuleReloaded(EngineModule module) {}
    public void NotifyGameplayContextChanged(GameplayContext context) {}
}

public struct AtomHandle
{
    public required long Rid;
    public required Spatial Parent;
    public required PhysicsComponent Component;
    public Vector3 WorldPosition;
    public Transform WorldTransform;
    public Vector3 LinearVelocity;
    public Vector3 AngularVelocity;
    public double GravityFactor;

    public bool HasColliders;
    public List<ColliderSphereComponent> SphereColliders;

    public double BoundingSphereRadius;
    public required List<CollisionCandidate> CollisionCandidates;
}

public struct CollisionCandidate
{
    public AtomHandle OtherHandle;
    public double OverlapDistance;
}
