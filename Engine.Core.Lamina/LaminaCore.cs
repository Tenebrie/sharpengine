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
    public Vector2 SpaceAvailable { get; }
    
    public void PushWidget(IWidget widget);
    public void PopWidget();
    
    public int RenderRequest(LaminaRenderRequest request);
    public int RenderText(LaminaTextRenderRequest request);
    public Vector2 MeasureText(LaminaTextRenderRequest request);
}

public interface ILaminaReflowContext
{
    public IWidget Parent { get; }
    public Vector2 OffsetToParent { get; }
    public Vector2 ChildrenPosition { get; set; }
    public Vector2 SpaceTakenByChildren { get; set; }
    public Vector2 SpaceAvailable { get; }
    public LaminaRenderRequest GetRequest(int index);
    public void SetRequest(int index, LaminaRenderRequest request);
    public LaminaTextRenderRequest GetTextRequest(int index);
    public void SetTextRequest(int index, LaminaTextRenderRequest request);
    
    public void PushWidget(IWidget widget);
    public void PopWidget();
}

public interface IWidget : ISpatial
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; }
    public Vector2? ExplicitContentSize { get; }
    public Vector2 ContentSize { get; }
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
    public required LaminaRenderScript.UserData[] ShaderParams;
}

public struct LaminaTextRenderRequest
{
    public required string Font { get; set; }
    public required int Size { get; set; }
    public required string Text { get; set; }
    public required Vector2 Position { get; set; }
    public required Color Color { get; set; }
    
    public int ShadowBlur { get; set; }
}