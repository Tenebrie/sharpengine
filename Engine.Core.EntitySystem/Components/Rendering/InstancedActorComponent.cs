using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Materials.Meshes.Wireframe;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
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
    public BoundingSphereComponent BoundingSphere
    {
        get => _staticMeshHolder.BoundingSphere;
        set => _staticMeshHolder.BoundingSphere = value;
    }
    public RenderScript RenderScript { get; set; } = RenderScript.Default;

    private Material? _material = null;

    public Material Material
    {
        get => _material ?? MaterialAssetManager.FallbackMaterial;
        set => _material = value;
    }
    public List<TInstance> Instances { get; } = [];
    public int InstanceCount => Instances.Count;

    [Profile]
    public void AddInstance(Transform instanceTransform)
    {
        var instancedActor = Activator.CreateInstance<TInstance>();
        instancedActor.Transform = instanceTransform;
        instancedActor.MaterialInstance = Material?.Instantiate() ?? MaterialAssetManager.FallbackMaterialInstance;
        Instances.Add(instancedActor);
        instancedActor.ParentManager = this;
        AdoptChild(instancedActor);
    }
    
    public void RemoveInstance(ActorInstance instance)
    {
        if (!Instances.Contains(instance) || instance is not TInstance instancedActor)
            return;

        instance.QueueFree();
        Instances.Remove(instancedActor);
    }

    public bool IsOnScreen { get; set; }

    private int _maxInstancesSeen = 0;
    private Transform[] _transformPool = [];
    private Transform[] _sphereTransformPool = [];
    private MaterialInstance[] _materialPool = [];
    
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
        
        RenderScript.Render(Instances.Count, Mesh, _transformPool, Material, _materialPool);
        // for (var i = 0; i < Instances.Count; i++)
        // {
        //     var actor = Instances[i];
        //     if (!IsValid(actor))
        //         continue;
        //     BoundingSphere.Transform.MultiplyReverse(actor.WorldTransform, ref _sphereTransformPool[i]);
        // }
        // LineSphereMesh.Shared.Render((uint)Instances.Count, _sphereTransformPool, [WireframeMaterial.Shared], LineSphereMesh.ColorMode.AxisColor);
    }
}
