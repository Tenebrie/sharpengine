using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Attributes;
using Engine.Core.Common;
using Engine.Core.DataStructures;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Modules;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class StaticMeshComponent : ActorComponent, IRenderable, ICullable
{
    [Component] private StaticMeshHolder _staticMeshHolder;
    public StaticMesh StaticMesh
    {
        get => _staticMeshHolder.Mesh;
        set
        {
            _staticMeshHolder.Mesh = value;
        }
    }

    public Material Material
    {
        get => _staticMeshHolder.Material;
        set
        {
            _staticMeshHolder.Material = value;
        }
    }

    public MaterialInstance MaterialInstance
    {
        get => _staticMeshHolder.MaterialInstance;
        set
        {
            _staticMeshHolder.MaterialInstance = value;
        }
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

    private readonly FrameBufferedSingletonArray<TransformSnapshot> _worldTransformBuffer = new();
    private readonly FrameBufferedSingletonArray<MaterialInstanceSnapshot> _materialInstanceBuffer = new();
    public RenderRequest? ProduceRenderRequest()
    {
        return new RenderRequest
        {
            Mesh = StaticMesh,
            Material = Material,
            RenderScript = RenderScript,

            InstanceCount = 1,
            InstanceTransforms = _worldTransformBuffer.Produce(WorldTransform.Snapshot()),
            MaterialInstances = _materialInstanceBuffer.Produce(MaterialInstance.Snapshot()),

            SortOrder = SortOrder
        }; 
    }
    
    public long Rid = -1;
    
    [OnReady]
    [OnModuleReload(EngineModule.Rendering)]
    protected void OnRegisterOnRenderingServer()
    { 
        var renderingModule = Backstage.RenderingModule;
        if (renderingModule == null)
            return;
        Rid = renderingModule.Register(this);
    }
    
    [OnUpdate]
    protected void OnReregisterOnRenderingServer()
    {
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UpdateRegistered(Rid, this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnRenderingServer()
    {
        if (Rid == -1)
            return;
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UnregisterRenderable(Rid);
    }
}
