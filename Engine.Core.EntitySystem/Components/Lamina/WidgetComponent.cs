using Engine.Core.Attributes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Core.Modules;

namespace Engine.Core.EntitySystem.Components.Lamina;

public partial class WidgetComponent : Actor, IWidget
{
    private LaminaLayout? _currentLayout;

    public Box BoundingBox
    {
        get
        {
            if (_currentLayout == null)
                return Box.Zero;

            var boxMin = Position;
            var boxMax = Position + Size;
            
            foreach (var child in GetChildren<WidgetComponent>())
            {
                var childBox = child.BoundingBox;
                boxMin.X = Math.Min(boxMin.X, childBox.Left);
                boxMin.Y = Math.Min(boxMin.Y, childBox.Top);
                boxMax.X = Math.Max(boxMax.X, childBox.Right);
                boxMax.Y = Math.Max(boxMax.Y, childBox.Bottom);
            }
            return new Box(boxMin.X, boxMin.Y, boxMax.X, boxMax.Y);
        }
    }

    // TODO: Rely on existing local -> global transformations?
    // Currently Transform is global (due to context)
    public Vector2 Position = Vector2.Zero;
    public Vector2 Size = Vector2.Zero;

    public void Initialize(LaminaLayout layout)
    {
        PopulateIntrinsics(layout);
        if (_currentLayout != null && _currentLayout.Equals(layout))
            return;
        
        _currentLayout = layout;
        InitializeChildren(layout);
    }
    
    private void SetLayoutWithIntrinsics(LaminaLayout layout)
    {
        _currentLayout = layout;
        PopulateIntrinsics(layout);   
    }
    
    private void InitializeChildren(LaminaLayout layout)
    {
        var shapeMatched = ChildrenShapeMatches(layout);
        
        while (!shapeMatched && Children.Count > 0) 
            Children[0].QueueFree();

        for (var index = 0; index < layout.Children.Count; index++)
        {
            var childLayout = layout.Children[index];
            WidgetComponent widget;
            if (shapeMatched)
            {
                widget = (WidgetComponent)Children[index];
                widget.SetLayoutWithIntrinsics(childLayout);
            }
            else
            {
                if (!LaminaRendererRepository.TryGet(childLayout, out var renderer))
                    throw new InvalidOperationException($"No renderer registered for layout of type {childLayout.GetType().Name}");
                var newWidget = (WidgetComponent?)Activator.CreateInstance(renderer.WidgetType);
                widget = newWidget ?? throw new Exception($"Failed to create instance of widget type {renderer.WidgetType.Name}");
                widget.SetLayoutWithIntrinsics(childLayout);
                AdoptChild(widget);
            }

            widget.InitializeChildren(childLayout);
        }
    }

    private bool ChildrenShapeMatches(LaminaLayout layout)
    {
        if (Children.Count != layout.Children.Count)
            return false;
        
        for (var i = 0; i < Children.Count; i++)
        {
            var childLayout = layout.Children[i];
            if (!LaminaRendererRepository.TryGet(childLayout, out var renderer))
                throw new InvalidOperationException($"No renderer registered for layout of type {childLayout.GetType().Name}");
            
            if (Children[i].GetType() != renderer.WidgetType)
                return false;
        }

        return true;
    }

    protected virtual void PopulateIntrinsics(LaminaLayout layout) {}
    protected virtual void Render(LaminaLayout layout, ILaminaRenderContext context) {}
    protected virtual void PostRender(LaminaLayout layout, ILaminaRenderContext context) {}
    public void PerformRender(ILaminaRenderContext context)
    {
        if (_currentLayout == null)
            return;

        try
        {
            Render(_currentLayout, context);
            foreach (var child in Children)
            {
                if (child is WidgetComponent widget)
                    widget.PerformRender(context);
            }
            PostRender(_currentLayout, context);
        } catch (Exception e)
        {
            Logger.Error("Exception during widget rendering: " + e.Message, e);
        }
    }
}