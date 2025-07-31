using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.Wireframe;
using Engine.Assets.Meshes;
using Engine.Assets.Meshes.Builtins;
using Engine.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Interfaces;
using JetBrains.Annotations;

namespace Engine.Worlds.Components;

public interface IInstancedActorComponent
{
    public void AddInstance(Transform instanceTransform);
    public void RemoveInstance(ActorInstance instance);
}

[UsedImplicitly]
public class InstancedActorComponent<TInstance> : ActorComponent, IInstancedActorComponent, IRenderable where TInstance : ActorInstance, new()
{
    public StaticMesh Mesh { get; set; }
    public MaterialInstance Material { get; set; }
    public List<ActorInstance> Instances { get; set; } = [];
    public int InstanceCount => Instances.Count;

    [Component] public BoundingSphereComponent BoundingSphere;

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

    private int MaxInstancesSeen = 0;
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
        
        if (Instances.Count > MaxInstancesSeen)
        {
            Array.Resize(ref _transformPool, Instances.Count);
            Array.Resize(ref _sphereTransformPool, Instances.Count);
            for (var i = MaxInstancesSeen; i < Instances.Count; i++)
            {
                _transformPool[i] = Transform.Identity;
                _sphereTransformPool[i] = Transform.Identity;
            }
            MaxInstancesSeen = Instances.Count;
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
        BoundingSphereMesh.PrepareRender((uint)Instances.Count, ref _sphereTransformPool, ref renderContext);
    }

    public void Render(ref RenderContext renderContext)
    {
        Mesh.Render((uint)Instances.Count, Material, ref renderContext);
        BoundingSphere.Mesh.Render((uint)Instances.Count, WireframeMaterial.Instance, ref renderContext);
    }
}
