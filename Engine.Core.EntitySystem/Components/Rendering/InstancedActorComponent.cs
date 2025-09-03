using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

public interface IInstancedActorComponent
{
    public void DestroyInstance(ActorInstance instance);
}

[UsedImplicitly]
public partial class InstancedActorComponent<TInstance> : ActorComponent, IInstancedActorComponent where TInstance : ActorInstance, new()
{
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

    private Material? _material = null;

    public Material InstanceMaterial
    {
        get => _material ?? MaterialAssetManager.FallbackMaterial;
        set => _material = value;
    }
    public List<TInstance> Instances { get; } = [];
    private List<InstancedActorCluster<TInstance>> Clusters { get; } = [];
    public int InstanceCount => Instances.Count;
    
    public TInstance CreateInstance()
    {
        InstancedActorCluster<TInstance> cluster;
        if (Clusters.Count == 0)
        {
            cluster = new InstancedActorCluster<TInstance>();
            Clusters.Add(cluster);
            cluster.InstanceManager = this;
            AdoptChild(cluster);
        }
        else
        {
            cluster = Clusters[0];
        }

        var instance = cluster.CreateInstance();
        Instances.Add(instance);
        return instance;
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
public partial class InstancedActorCluster<TInstance> : ActorComponent, IRenderable where TInstance : ActorInstance, new()
{
    internal InstancedActorComponent<TInstance> InstanceManager = null!;
    private List<TInstance> Instances { get; } = [];
    
    public Vector3 BoundingSphereWorldOrigin { get; private set; }
    public double BoundingSphereWorldRadius { get; private set; }

    private int _maxInstancesSeen = 0;
    private Transform[] _transformPool = [];
    private Transform[] _sphereTransformPool = [];
    private MaterialInstance[] _materialPool = [];
    
    public TInstance CreateInstance()
    {
        var instancedActor = Activator.CreateInstance<TInstance>();
        instancedActor.MaterialInstance = InstanceManager.InstanceMaterial.Instantiate();
        instancedActor.ParentManager = InstanceManager;
        Instances.Add(instancedActor);
        AdoptChild(instancedActor);
        RecomputeBoundingSphere();
        return instancedActor;
    }
    
    public void RemoveInstance(ActorInstance instance)
    {
        if (!Instances.Contains(instance) || instance is not TInstance instancedActor)
            return;

        Instances.Remove(instancedActor);
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
        BoundingSphereWorldRadius = radius;
    }
    
    public RenderRequest ProduceRenderRequest()
    {
        if (Instances.Count > _maxInstancesSeen)
        {
            Array.Resize(ref _transformPool, Instances.Count);
            Array.Resize(ref _sphereTransformPool, Instances.Count);
            Array.Resize(ref _materialPool, Instances.Count);
            for (var i = _maxInstancesSeen; i < Instances.Count; i++)
            {
                _transformPool[i] = Transform.Identity;
                _sphereTransformPool[i] = Transform.Identity;
                _materialPool[i] = MaterialAssetManager.FallbackMaterialInstance;
            }
            _maxInstancesSeen = Instances.Count;
        }
        for (var i = 0; i < Instances.Count; i++)
        {
            var actor = Instances[i];
            if (!IsValid(actor))
                continue;
            _transformPool[i] = actor.WorldTransform;
            _materialPool[i] = actor.MaterialInstance;
        }
        
        return new RenderRequest
        {
            Mesh = InstanceManager.InstanceStaticMesh,
            Material = InstanceManager.InstanceMaterial,
            RenderScript = InstanceManager.InstanceRenderScript,

            InstanceCount = Instances.Count,
            InstanceTransforms = _transformPool,
            MaterialInstances = _materialPool
        };
    }
}
