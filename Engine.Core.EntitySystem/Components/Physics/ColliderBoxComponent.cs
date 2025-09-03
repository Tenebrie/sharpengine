using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Physics;

[UsedImplicitly]
public partial class ColliderBoxComponent : ActorComponent
{
    public Vector3 WorldPosition => WorldTransform.Position;
    public Vector3 WorldSize => WorldTransform.Scale;

    public StaticMesh StaticMesh => LineSphereMesh.Shared;
    public Material Material => Material.CreateFromDisk("Shaders/cube");
    public IRenderScript RenderScript => IRenderScript.Default;

    public bool IsOnScreen { get; set; }
    // public void PerformCulling(Camera activeCamera) => IsOnScreen = activeCamera.SphereInFrustum(WorldTransform, 0.7072, null);
    public void PerformCulling(Camera activeCamera) => IsOnScreen = false;
    
    public Vector3 BoundingSphereWorldOrigin => WorldTransform.Position;
    public double BoundingSphereWorldRadius => 1;
    
    public int GetInstanceCount() => 1;
    
    private readonly Transform[] _singleComponentTransforms = new Transform[1];

    public RenderRequest Render()
    {
        _singleComponentTransforms[0] = WorldTransform;
        return new RenderRequest
        {
            Mesh = StaticMesh,
            Material = Material,
            RenderScript = RenderScript,

            InstanceCount = 1,
            InstanceTransforms = _singleComponentTransforms,
            MaterialInstances = [Material.Instantiate()]
        };
        // LineSphereMesh.Shared.Render(1, _singleComponentTransforms, [WireframeMaterial.Shared], LineSphereMesh.ColorMode.Collider);
    }
}
