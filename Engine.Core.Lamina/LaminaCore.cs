using System.Drawing;
using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.Modules;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Lamina;

public struct LaminaWidgetRenderer : ILaminaWidgetRenderer
{
    public required Type LayoutType; // LaminaLayout extending type
    public required Type WidgetType; // IWidget implementing type
}

public interface ILaminaRenderContext
{
    public Vector2 Position { get; set; }
    IDeviceContext DeviceContext { get; }
    public void RenderText(string font, int size, string text, Vector2 position, Color color, int shadowBlur = 0);
    public void RenderRequest(LaminaRenderRequest request);
}

public interface IWidget : IAtom
{
     
}

public struct LaminaRenderRequest
{
    public required StaticMesh Mesh;
    public required Material Material;
    public required IRenderScript RenderScript;
    
    public required int InstanceCount;
    public required Transform[] InstanceTransforms;
    public required MaterialInstance[] MaterialInstances;
    
    public int HashCode => Mesh.GetHashCode() ^ Material.GetHashCode() ^ RenderScript.GetHashCode();
}