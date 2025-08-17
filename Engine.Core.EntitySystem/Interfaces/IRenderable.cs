using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable
{
    public StaticMesh Mesh { get; }
    public Material Material { get; }
    public IRenderScript RenderScript { get; }
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera);
    public RenderRequest Render();
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
