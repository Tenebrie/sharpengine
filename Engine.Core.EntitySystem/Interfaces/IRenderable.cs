using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable
{
    public RenderRequest ProduceRenderRequest();
    public Vector3 BoundingSphereWorldOrigin { get; }
    public double BoundingSphereWorldRadius { get; }
}

public struct RenderRequest
{
    public required StaticMesh Mesh;
    public required Material Material;
    public required IRenderScript RenderScript;
    
    public required int InstanceCount;
    public required Transform[] InstanceTransforms;
    public required MaterialInstance[] MaterialInstances;
    
    public int HashCode => Mesh.GetHashCode() ^ Material.GetHashCode() ^ RenderScript.GetHashCode();
}
