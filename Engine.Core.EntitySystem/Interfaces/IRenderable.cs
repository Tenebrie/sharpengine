using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.Lamina;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable
{
    public RenderRequest ProduceRenderRequest();
}

public interface ILaminaRenderable
{
    public bool Dirty { get; set; }
    public void EnsureRenderTarget();
    public void CollectCommandList(ILaminaRenderContext renderContext);
    public ITexture RenderTarget { get; }
    public ITextureView RenderTargetView { get; }
    public ITextureView ShaderResourceView { get; }
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
