using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Profiling;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Components;

public interface IInstancedActorComponent
{
    StaticMesh Mesh { get; set; }
    Material Material { get; set; }
    
    List<ActorInstance> Instances { get; set; }
    
    void AddInstance(Transform instanceTransform);
}

public class InstancedActorComponent<TInstance> : ActorComponent, IInstancedActorComponent where TInstance : ActorInstance, new()
{
    public StaticMesh Mesh { get; set; }
    public Material Material { get; set; }
    public List<ActorInstance> Instances { get; set; } = [];

    [Profile]
    public void AddInstance(Transform instanceTransform)
    {
        var instancedActor = Activator.CreateInstance<TInstance>();
        AdoptChild(instancedActor);
        instancedActor.Transform = instanceTransform;
        Instances.Add(instancedActor);
    }
}
