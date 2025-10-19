using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Components.Lamina;

public abstract partial class LaminaWidgetComponent<T> : WidgetComponent
{
    public abstract void OnRender(T layout, ILaminaRenderContext context);
    public virtual void OnRenderChildren(T layout, ILaminaRenderContext context)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            if (child is WidgetComponent widget)
                widget.PerformRender(context);
        }
    }
    public virtual void OnPostRender(T layout, ILaminaRenderContext context) {}
    public virtual void OnPopulateIntrinsics(T layout) {}
}
public partial class RootWidgetComponent : LaminaWidgetComponent<LaminaLayout>
{
    public override void OnRender(LaminaLayout layout, ILaminaRenderContext context)
    {
        
    }
}

public partial class WidgetComponent : Actor, IWidget
{
    private LaminaLayout? _currentLayout;
    // public Box BoundingBox
    // {
    //     get
    //     {
    //         if (_currentLayout == null)
    //             return Box.Zero;
    //
    //         var boxMin = Position;
    //         var boxMax = Position + Size;
    //         
    //         foreach (var child in GetChildren<WidgetComponent>())
    //         {
    //             var childBox = child.BoundingBox;
    //             boxMin.X = Math.Min(boxMin.X, childBox.Left);
    //             boxMin.Y = Math.Min(boxMin.Y, childBox.Top);
    //             boxMax.X = Math.Max(boxMax.X, childBox.Right);
    //             boxMax.Y = Math.Max(boxMax.Y, childBox.Bottom);
    //         }
    //         return new Box(boxMin.X, boxMin.Y, boxMax.X, boxMax.Y);
    //     }
    // }

    // TODO: Rely on existing local -> global transformations?
    // Currently Transform is global (due to context)
    // public Vector2 Position = Vector2.Zero;
    // public Vector2 Size = Vector2.Zero;

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

    private void PopulateIntrinsics(LaminaLayout layout)
    {
        // TODO: Optimize dynamic calls?
        ((dynamic)this).OnPopulateIntrinsics((dynamic)layout);
    }

    private void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        ((dynamic)this).OnRender((dynamic)layout, context);
    }

    private void RenderChildren(LaminaLayout layout, ILaminaRenderContext context)
    {
        ((dynamic)this).OnRenderChildren((dynamic)layout, context);
    }

    private void PostRender(LaminaLayout layout, ILaminaRenderContext context)
    {
        ((dynamic)this).OnPostRender((dynamic)layout, context);
    }
    public void PerformRender(ILaminaRenderContext context)
    {
        if (_currentLayout == null)
            return;

        context.PushWidget(this);
        
        try
        {
            Render(_currentLayout, context);
            RenderChildren(_currentLayout, context);
            PostRender(_currentLayout, context);
        }
        catch (Exception e)
        {
            Logger.Error("Exception during widget rendering: " + e.Message, e);
        }
        finally
        {
            context.PopWidget();
        }
    }
}