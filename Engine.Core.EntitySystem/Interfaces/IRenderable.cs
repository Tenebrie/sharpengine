using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.Lamina;
using Engine.Core.Modules;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable : IMaskedRenderable
{
    public RenderRequest? ProduceRenderRequest();
}

public interface ILaminaRenderable : IMaskedLaminaRenderable
{
    public bool Dirty { get; set; }
    public void EnsureRenderTarget();
    public void CollectCommandList(ILaminaRenderContext renderContext);
    public Vector2 TextureSize { get; }
    public ITextureView RenderTargetView { get; }
    public ITextureView ShaderResourceView { get; }
}

public struct RenderRequest
{
    public required StaticMesh Mesh;
    public required Material Material;
    public required IRenderScript RenderScript;
    
    public required int InstanceCount;
    public required TransformSnapshot[] InstanceTransforms;
    public required MaterialInstanceSnapshot[] MaterialInstances;
    public int SortOrder { get; set; }
    
    public int HashCode => Mesh.GetHashCode() ^ Material.GetHashCode() ^ RenderScript.GetHashCode();
}
