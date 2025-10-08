using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class StaticMeshComponent : ActorComponent, IRenderable, ICullable
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
    public int SortOrder { get; set; } = 0;
    private IRenderScript RenderScript { get; set; } = IRenderScript.Default;
    public bool CullingEnabled { get; set; } = true;
    public Vector3 BoundingSphereWorldOrigin => WorldTransform.Position;
    public double BoundingSphereWorldRadius => BoundingSphere.WorldRadius;

    private readonly TransformSnapshot[] _singleComponentTransforms = new TransformSnapshot[1];
    private RenderRequest? _renderRequest;
    
    public RenderRequest ProduceRenderRequest()
    {
        if (_singleComponentTransforms == null)
            throw new NullReferenceException($"{nameof(_singleComponentTransforms)} was null");
        if (WorldTransform == null)
            throw new NullReferenceException($"{nameof(WorldTransform)} was null");
        _singleComponentTransforms[0] = WorldTransform.Snapshot();
        if (_renderRequest != null)
            return (RenderRequest)_renderRequest;
        
        _renderRequest = new RenderRequest
        {
            Mesh = StaticMesh,
            Material = Material,
            RenderScript = RenderScript,

            InstanceCount = 1,
            InstanceTransforms = _singleComponentTransforms,
            MaterialInstances = [MaterialInstance.Snapshot()],
            
            SortOrder = SortOrder
        };
        
        return (RenderRequest)_renderRequest;
    }
}
