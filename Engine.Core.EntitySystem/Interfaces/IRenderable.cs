using System.Drawing;
using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.Lamina;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable : IAtom, IMaskedRenderable
{
    public RenderRequest? ProduceRenderRequest();
}

public interface ILaminaRenderable : IAtom, IMaskedLaminaRenderable
{
    public bool Dirty { get; set; }
    public void EnsureRenderTarget(ILaminaRenderContext renderContext, ILaminaReflowContext reflowContext);
    public void CollectCommandList(ILaminaRenderContext renderContext, ILaminaReflowContext reflowContext);
    public void Rerender();
    public Vector2 InternalTextureSize { get; }
    public Color BackgroundColor { get; }
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
    public object? ExtraShaderParams;
    
    public required bool IsCullable { get; init; }
    public int SortOrder { get; init; }
    
    public int HashCode => Mesh.GetHashCode() ^ Material.GetHashCode() ^ RenderScript.GetHashCode() ^ SortOrder;
}
