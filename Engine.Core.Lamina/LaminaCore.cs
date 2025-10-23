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
    public Vector2 OffsetToParent { get; }
    public Vector2 ChildrenPosition { get; set; }
    public IWidget Parent { get; }
    
    public void PushWidget(IWidget widget);
    public void PopWidget();
    
    public void RenderText(string font, int size, string text, Vector2 position, Color color, int shadowBlur = 0);
    public void RenderRequest(LaminaRenderRequest request);
}

public interface IWidget : ISpatial
{
     
}

public struct LaminaRenderRequest
{
    public required StaticMesh Mesh;
    public required Material Material;
    public required LaminaRenderScript RenderScript;
    
    public required int InstanceCount;
    public required TransformSnapshot[] InstanceTransforms;
    public required MaterialInstanceSnapshot[] MaterialInstances;
    
    public Rect? ScissorRect;
    public required LaminaRenderScript.UserData ShaderParams;
}