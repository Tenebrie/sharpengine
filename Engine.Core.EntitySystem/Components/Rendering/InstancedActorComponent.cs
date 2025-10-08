using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using Engine.Core.Profiling.Attributes;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

public interface IInstancedActorComponent
{
    public void DestroyInstance(ActorInstance instance);
}

[UsedImplicitly]
public partial class InstancedActorComponent<TInstance> : ActorComponent, IInstancedActorComponent where TInstance : ActorInstance, new()
{
    internal const double ClusterSize = 100.0;
    internal const double ClusterSizeSquared = ClusterSize * ClusterSize;
    
    [Component] private StaticMeshHolder _instanceStaticMeshHolder;
    public StaticMesh InstanceStaticMesh
    {
        get => _instanceStaticMeshHolder.Mesh;
        set => _instanceStaticMeshHolder.Mesh = value;
    }
    public BoundingSphereComponent InstanceBoundingSphere
    {
        get => _instanceStaticMeshHolder.BoundingSphere;
        set => _instanceStaticMeshHolder.BoundingSphere = value;
    }
    public IRenderScript InstanceRenderScript { get; set; } = IRenderScript.Default;
    public bool CullingEnabled { get; set; } = true;

    private Material? _material = null;

    public Material InstanceMaterial
    {
        get => _material ?? MaterialAssetManager.FallbackMaterial;
        set => _material = value;
    }
    public List<TInstance> Instances { get; } = [];
    private List<InstancedActorCluster<TInstance>> Clusters { get; } = [];
    public int InstanceCount => Instances.Count;

    private int _currentUpdatedCluster = 0;
    
    [OnTimer(Seconds = 0.005)]
    protected void OnUpdate()
    {
        if (Clusters.Count == 0)
            return;
        if (_currentUpdatedCluster >= Clusters.Count)
            _currentUpdatedCluster = 0;
        
        Clusters[_currentUpdatedCluster].CheckClusterValidity(out var evictedInstances);
        foreach (var instance in evictedInstances)
        {
            var cluster = FindClusterForInstance(instance);
            cluster.AssignInstance(instance);
        }
        if (Clusters[_currentUpdatedCluster].Redundant)
        {
            Clusters[_currentUpdatedCluster].QueueFree();
            Clusters.RemoveAt(_currentUpdatedCluster);
            _currentUpdatedCluster--;
        }
        _currentUpdatedCluster = (_currentUpdatedCluster + 1) % Clusters.Count;
    }
    
    public TInstance CreateInstance()
    {
        var instance = Activator.CreateInstance<TInstance>();
        var cluster = FindClusterForInstance(instance);

        instance.MaterialInstance = InstanceMaterial.Instantiate();
        instance.ParentManager = this;
        Instances.Add(instance);
        AdoptChild(instance);
        cluster.AssignInstance(instance);
        return instance;
    }
    
    private InstancedActorCluster<TInstance> FindClusterForInstance(TInstance instance)
    {
        foreach (var cluster in Clusters.Where(x => x.InstanceCount < 100).OrderBy(x => x.InstanceCount))
        {
            var distanceSquared = instance.WorldTransform.Position.DistanceSquaredTo(cluster.BoundingSphereWorldOrigin);
            if (distanceSquared <= ClusterSizeSquared)
                return cluster;
        }

        var newCluster = new InstancedActorCluster<TInstance>();
        Clusters.Add(newCluster);
        newCluster.InstanceManager = this;
        AdoptChild(newCluster);
        return newCluster;
    }
    
    public void DestroyInstance(ActorInstance instance)
    {
        if (!Instances.Contains(instance) || instance is not TInstance instancedActor)
            return;

        instance.QueueFree();
        Instances.Remove(instancedActor);
        
        foreach (var cluster in Clusters)
            cluster.RemoveInstance(instance);
    }
}

[UsedImplicitly]
internal partial class InstancedActorCluster<TInstance> : ActorComponent, IRenderable, ICullable where TInstance : ActorInstance, new()
{
    internal InstancedActorComponent<TInstance> InstanceManager = null!;
    private List<TInstance> Instances { get; } = [];
    private ReaderWriterLockSlim InstanceLock { get; } = new();
    public bool CullingEnabled { get; set; } = true;
    
    public Vector3 BoundingSphereWorldOrigin { get; private set; }
    public double BoundingSphereWorldRadius { get; private set; }
    
    public bool Redundant => Instances.Count == 0;
    public int InstanceCount => Instances.Count;

    private int _maxInstancesSeen = 0;
    private TransformSnapshot[] _transformPool = [];
    private TransformSnapshot[] _sphereTransformPool = [];
    private MaterialInstanceSnapshot[] _materialPool = [];
    
    public void AssignInstance(TInstance instance)
    {
        InstanceLock.EnterWriteLock();
        Instances.Add(instance);
        InstanceLock.ExitWriteLock();
        RecomputeBoundingSphere();
    }
    
    public void RemoveInstance(ActorInstance instance)
    {
        if (!Instances.Contains(instance) || instance is not TInstance instancedActor)
            return;

        InstanceLock.EnterWriteLock();
        Instances.Remove(instancedActor);
        InstanceLock.ExitWriteLock();
        RecomputeBoundingSphere();
    }

    private void RecomputeBoundingSphere()
    {
        if (Instances.Count == 0)
        {
            BoundingSphereWorldOrigin = Vector3.Zero;
            BoundingSphereWorldRadius = 0;
            return;
        }
        
        InstanceLock.EnterReadLock();
        var center = Vector3.Zero;
        foreach (var instance in Instances)
        {
            center += instance.WorldTransform.Position;
        }
        center /= Instances.Count;
        
        var radius = 0.0;
        foreach (var instance in Instances)
        {
            var distance = (instance.WorldTransform.Position - center).Length + InstanceManager.InstanceBoundingSphere.WorldRadius;
            if (distance > radius)
                radius = distance;
        }

        BoundingSphereWorldOrigin = center;
        BoundingSphereWorldRadius = radius + 10.0; // Padding
        InstanceLock.ExitReadLock();
    }
    
    public void CheckClusterValidity(out List<TInstance> evicted)
    {
        const double clusterSize = InstancedActorComponent<TInstance>.ClusterSize;
        const double maxErrorSquared = clusterSize * clusterSize * 2;
        var badInstances = Instances
            .Where(instance =>
                instance.WorldTransform.Position.DistanceSquaredTo(BoundingSphereWorldOrigin) > maxErrorSquared)
            .ToList();
        foreach (var badInstance in badInstances)
        {
            RemoveInstance(badInstance);
        }
        evicted = badInstances.ToList();
        
        // Recompute the sphere to account for instances removed or just moving around
        RecomputeBoundingSphere();
    }
    
    public RenderRequest ProduceRenderRequest()
    {
        InstanceLock.EnterReadLock();
        var instanceCount = Instances.Count;
        if (instanceCount > _maxInstancesSeen)
        {
            Array.Resize(ref _transformPool, instanceCount);
            Array.Resize(ref _sphereTransformPool, instanceCount);
            Array.Resize(ref _materialPool, instanceCount);
            for (var i = _maxInstancesSeen; i < instanceCount; i++)
            {
                _transformPool[i] = TransformSnapshot.Identity;
                _sphereTransformPool[i] = TransformSnapshot.Identity;
                _materialPool[i] = MaterialAssetManager.FallbackMaterialInstance.Snapshot();
            }
            _maxInstancesSeen = instanceCount;
        }
        for (var i = 0; i < instanceCount; i++)
        {
            var actor = Instances[i];
            if (!IsValid(actor))
                continue;
            _transformPool[i] = actor.WorldTransform.Snapshot();
            _materialPool[i] = actor.MaterialInstance.Snapshot();
        }
        InstanceLock.ExitReadLock();
        
        return new RenderRequest
        {
            Mesh = InstanceManager.InstanceStaticMesh,
            Material = InstanceManager.InstanceMaterial,
            RenderScript = InstanceManager.InstanceRenderScript,

            InstanceCount = instanceCount,
            InstanceTransforms = _transformPool,
            MaterialInstances = _materialPool
        };
    }
}
