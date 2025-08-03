using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Profiling;
using Engine.Module.Physics.Utilities;
using Engine.Module.Physics.WorkerThreads;
using JetBrains.Annotations;

namespace Engine.Module.Physics;

[UsedImplicitly]
public class PhysicsModule : IPhysicsModule
{
    private readonly PhysicsTaskDispatcher _dispatcher = new();
    private readonly AtomRegistrationHandler _registeredAtoms = new();
    private readonly CacheRevalidationServiceHandler _revalidationServices = new();

    private readonly WorkerPool _workerPool = new();

    public void RegisterService(CacheRevalidationService service) => _revalidationServices.Add(service);
    public void UnregisterService(CacheRevalidationService service) => _revalidationServices.Remove(service);

    public void Initialize() => _workerPool.Initialize();
    public void Shutdown() => _workerPool.Shutdown();
    
    public long Register(Spatial parent, PhysicsComponent component) => _registeredAtoms.Add(parent, component);
    public void Unregister(long rid) => _registeredAtoms.Remove(rid);

    public void ProcessPhysicsFrame(double deltaTime)
    {
        var stopwatch = Profiler.Start();
        var atoms = _registeredAtoms.AsArray();
        
        _revalidationServices.DisableAll();
        
        _dispatcher.Dispatch(_workerPool, deltaTime, PhysicsTaskDispatcher.PhysicsTaskType.CollectData, atoms);
        _dispatcher.Dispatch(_workerPool, deltaTime, PhysicsTaskDispatcher.PhysicsTaskType.InitialMove, atoms);
        _dispatcher.Dispatch(_workerPool, deltaTime, PhysicsTaskDispatcher.PhysicsTaskType.FlushTransform, atoms);
        
        _revalidationServices.EnableAll();
        stopwatch.StopAndReport(GetType(), ProfilingContext.PhysicsUpdate);
    }

    public void RevalidateWorldTransform(Spatial atom) => WorkerPoolMember.RevalidateWorldTransform(atom);
}

public struct AtomHandle
{
    public Spatial Parent;
    public Transform WorldTransform;
    public Vector3 Velocity;
    public PhysicsComponent Component;
    public List<ColliderSphereComponent> SphereColliders;
}
