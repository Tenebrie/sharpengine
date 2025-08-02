using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Materials.Meshes.Wireframe;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

public interface IInstancedActorComponent
{
    public void AddInstance(Transform instanceTransform);
    public void RemoveInstance(ActorInstance instance);
}

[UsedImplicitly]
public partial class InstancedActorComponent<TInstance> : ActorComponent, IInstancedActorComponent, IRenderable where TInstance : ActorInstance, new()
{
    [Component] private StaticMeshHolder _staticMeshHolder;
    public StaticMesh Mesh
    {
        get => _staticMeshHolder.Mesh;
        set => _staticMeshHolder.Mesh = value;
    }
    public MaterialInstance Material
    {
        get => _staticMeshHolder.Material;
        set => _staticMeshHolder.Material = value;
    }
    public BoundingSphereComponent BoundingSphere
    {
        get => _staticMeshHolder.BoundingSphere;
        set => _staticMeshHolder.BoundingSphere = value;
    }
    public List<ActorInstance> Instances { get; } = [];
    public int InstanceCount => Instances.Count;

    [Profile]
    public void AddInstance(Transform instanceTransform)
    {
        var instancedActor = Activator.CreateInstance<TInstance>();
        AdoptChild(instancedActor);
        instancedActor.Transform = instanceTransform;
        Instances.Add(instancedActor);
        instancedActor.ParentManager = this;
    }
    
    public void RemoveInstance(ActorInstance instance)
    {
        if (instance == null || !Instances.Contains(instance))
            return;

        instance.QueueFree();
        Instances.Remove(instance);
    }

    public bool IsOnScreen { get; set; }

    private int _maxInstancesSeen = 0;
    private Transform[] _transformPool = [];
    private Transform[] _sphereTransformPool = [];
    
    public void PerformCulling(Camera activeCamera)
    {
        IsOnScreen = false;
        foreach (var actor in Instances)
        {
            actor.IsOnScreen = activeCamera.SphereInFrustum(BoundingSphere, actor.Transform);
            if (actor.IsOnScreen)
                IsOnScreen = true;
        }
    }
    public int GetInstanceCount() => Instances.Count * 2;

    public void PrepareRender(ref RenderContext renderContext)
    {
        if (Mesh == null)
        {
            Logger.Error("InstancedActorComponent: Mesh is null, cannot render.");
            return;
        }
        
        if (Instances.Count > _maxInstancesSeen)
        {
            Array.Resize(ref _transformPool, Instances.Count);
            Array.Resize(ref _sphereTransformPool, Instances.Count);
            for (var i = _maxInstancesSeen; i < Instances.Count; i++)
            {
                _transformPool[i] = Transform.Identity;
                _sphereTransformPool[i] = Transform.Identity;
            }
            _maxInstancesSeen = Instances.Count;
        }
        for (var i = 0; i < Instances.Count; i++)
        {
            var actor = Instances[i];
            if (!IsValid(actor))
                continue;
            _transformPool[i] = actor.WorldTransform;
        }

        Mesh.PrepareRender((uint)Instances.Count, ref _transformPool, ref renderContext);
        for (var i = 0; i < Instances.Count; i++)
        {
            var actor = Instances[i];
            if (!IsValid(actor))
                continue;
            BoundingSphere.Transform.MultiplyReverse(actor.WorldTransform, ref _sphereTransformPool[i]);
        }
        SphereMesh.PrepareRender((uint)Instances.Count, ref _sphereTransformPool, ref renderContext);
    }

    public void Render(ref RenderContext renderContext)
    {
        Mesh.Render((uint)Instances.Count, Material, ref renderContext);
        BoundingSphere.Mesh.Render((uint)Instances.Count, WireframeMaterial.Instance, ref renderContext);
    }
}
