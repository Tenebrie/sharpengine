using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.Wireframe;
using Engine.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Interfaces;
using JetBrains.Annotations;

namespace Engine.Worlds.Components;

[UsedImplicitly]
public class InstancedActorComponent<TInstance> : ActorComponent, IRenderable where TInstance : ActorInstance, new()
{
    public StaticMesh Mesh { get; set; }
    public Material Material { get; set; }
    public List<ActorInstance> Instances { get; set; } = [];

    [Component] public BoundingSphereComponent BoundingSphere;

    [Profile]
    public void AddInstance(Transform instanceTransform)
    {
        var instancedActor = Activator.CreateInstance<TInstance>();
        AdoptChild(instancedActor);
        instancedActor.Transform = instanceTransform;
        Instances.Add(instancedActor);
    }

    public bool IsOnScreen { get; set; }

    private Transform[] _transformPool = [];
    
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
    
    public void Render()
    {
        Array.Resize(ref _transformPool, Instances.Count);
        for (var i = 0; i < Instances.Count; i++)
        {
            var actor = Instances[i];
            _transformPool[i] = actor.WorldTransform;
        }

        Mesh.Render((uint)Instances.Count, ref _transformPool, 0, Material);
        for (var i = 0; i < Instances.Count; i++)
        {
            var actor = Instances[i];
            _transformPool[i] = BoundingSphere.Transform * actor.WorldTransform;
        }
        BoundingSphere.Mesh.Render((uint)Instances.Count, ref _transformPool, 0, WireframeMaterial.Instance);
    }
}
