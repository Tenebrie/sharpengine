using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class StaticMeshComponent : ActorComponent, IRenderable
{
    [Component] private StaticMeshHolder _staticMeshHolder;
    public StaticMesh StaticMesh
    {
        get => _staticMeshHolder.Mesh;
        set => _staticMeshHolder.Mesh = value;
    }
    public Material Material
    {
        get => _staticMeshHolder.Material;
        set => _staticMeshHolder.Material = value;
    }
    public MaterialInstance MaterialInstance
    {
        get => _staticMeshHolder.MaterialInstance;
        set => _staticMeshHolder.MaterialInstance = value;
    }
    public BoundingSphereComponent BoundingSphere
    {
        get => _staticMeshHolder.BoundingSphere;
        set => _staticMeshHolder.BoundingSphere = value;
    }
    public IRenderScript RenderScript { get; set; } = IRenderScript.Default;
    
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(BoundingSphere, null);
    
    private readonly Transform[] _singleComponentTransforms = new Transform[1];
    private RenderRequest? _renderRequest;
    
    public RenderRequest Render()
    {
        _singleComponentTransforms[0] = WorldTransform;
        if (_renderRequest != null)
            return (RenderRequest)_renderRequest;
        
        _renderRequest = new RenderRequest
        {
            Mesh = StaticMesh,
            Material = Material,
            RenderScript = RenderScript,

            InstanceCount = 1,
            InstanceTransforms = _singleComponentTransforms,
            MaterialInstances = [MaterialInstance]
        };
        
        return (RenderRequest)_renderRequest;
    }
}
