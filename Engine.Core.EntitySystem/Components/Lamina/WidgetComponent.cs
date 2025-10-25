using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Lamina;

[PublicAPI]
public abstract partial class LaminaWidgetComponent<T> : WidgetComponent where T : LaminaLayout
{
    public virtual void OnPopulateIntrinsics(T layout) {}
    public virtual void OnRender(T layout, ILaminaRenderContext context) {}
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
    public virtual void OnReflow(T layout, ILaminaReflowContext context) {}
    public virtual void OnReflowChildren(T layout, ILaminaReflowContext context)
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            if (child is WidgetComponent widget)
                widget.PerformReflow(context);
        }
    }
    public virtual void OnPostReflow(T layout, ILaminaReflowContext context) {}
}

[UsedImplicitly]
public partial class RootWidgetComponent : LaminaWidgetComponent<LaminaLayout>;

public partial class WidgetComponent : Actor, IWidget
{
    private LaminaLayout? _currentLayout;
    public LaminaLayout CurrentLayout => _currentLayout ?? throw new InvalidOperationException("Widget has not been initialized with a layout yet.");
    public Vector2 Position
    {
        get => Transform.Position.ToVector2();
        set => Transform.Position = new Vector3(value.X, value.Y, Transform.Position.Z);
    }

    public Vector2 Size 
    {
        get => Transform.Scale.ToVector2();
        set => Transform.Scale = new Vector3(value.X, value.Y, Transform.Scale.Z);
    }
    public Vector2? ExplicitSize { get; set; } = null;
    public Vector2 MinSize { get; set; } = new(0, 0);
    public Vector2 Padding { get; set; } = new(0, 0);
    public Vector2 ContentSize => MinSize - Padding * 2;
    public Vector2? ExplicitContentSize => ExplicitSize.HasValue ? ExplicitSize.Value - Padding * 2 : null;

    public void Initialize(LaminaLayout layout)
    {
        PopulateIntrinsics(layout);
        if (_currentLayout != null && _currentLayout.Equals(layout))
            return;

        if (layout.SharedProps.Size.X > 0 || layout.SharedProps.Size.Y > 0)
            ExplicitSize = layout.SharedProps.Size;
        _currentLayout = layout;
        InitializeChildren(layout);
    }
    
    private void SetLayoutWithIntrinsics(LaminaLayout layout)
    {
        if (layout.SharedProps.Size.X > 0 || layout.SharedProps.Size.Y > 0)
            ExplicitSize = layout.SharedProps.Size;
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
        if (layout.SharedProps.Size is { X: > 0, Y: > 0 } && layout.SharedProps.FillMode == LaminaFillMode.FillContainer)
            layout.SharedProps.FillMode = LaminaFillMode.None;
        ((dynamic)this).OnPopulateIntrinsics((dynamic)layout);
    }
    private void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        // Padding = layout.SharedProps.Padding;
        Size = ExplicitSize ?? Vector2.Zero;
        // MinSize = ExplicitSize ?? layout.SharedProps.Padding * 2;
        if (layout.SharedProps.FillMode == LaminaFillMode.FillContainer && context.Parent.ExplicitContentSize.HasValue)
            Size = context.Parent.ExplicitContentSize.Value;

        if (layout.SharedProps.FillMode == LaminaFillMode.FillContainer && context.Parent.ExplicitContentSize.HasValue && !ExplicitSize.HasValue)
        {
            Size = context.Parent.ExplicitContentSize.Value;
            ExplicitSize = context.Parent.ExplicitContentSize.Value;
        }
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
    private void Reflow(LaminaLayout layout, ILaminaReflowContext context)
    {
        var position = context.OffsetToParent + layout.SharedProps.Position;
        Position = position.Rounded();
        Padding = layout.SharedProps.Padding;
        context.ChildrenPosition = Padding;
        if (layout.SharedProps.FillMode == LaminaFillMode.FillContainer && !ExplicitSize.HasValue)
            Size = context.SpaceAvailable;
        else
            Size = ExplicitSize.HasValue ? Vector2.Max(ExplicitSize.Value, Size) : Size;
        
        ((dynamic)this).OnReflow((dynamic)layout, context);
    }
    private void ReflowChildren(LaminaLayout layout, ILaminaReflowContext context)
    {
        ((dynamic)this).OnReflowChildren((dynamic)layout, context);
    }
    private void PostReflow(LaminaLayout layout, ILaminaReflowContext context)
    {
        ((dynamic)this).OnPostReflow((dynamic)layout, context);
        var largestX = 0.0;
        var largestY = 0.0;
        foreach (var child in Children)
        {
            if (child is not WidgetComponent widget)
                continue;
            largestX = Math.Max(largestX, widget.Position.X + Math.Max(widget.MinSize.X, widget.Size.X));
            largestY = Math.Max(largestY, widget.Position.Y + Math.Max(widget.MinSize.Y, widget.Size.Y));
        }

        if (ExplicitSize.HasValue)
        {
            largestX = ExplicitSize.Value.X;
            largestY = ExplicitSize.Value.Y;
        }
        if (layout.SharedProps.FillMode == LaminaFillMode.FillContainer)
            MinSize = Vector2.Zero;
        else
            MinSize = (largestX, largestY) + Padding;
        Size = Vector2.Max(Size, MinSize);
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
            Logger.Error("Exception during widget render: " + e.Message, e);
        }
        context.PopWidget();
    }

    public void PerformReflow(ILaminaReflowContext context)
    {
        if (_currentLayout == null)
            return;
        
        context.PushWidget(this);
        
        try
        {
            Reflow(_currentLayout, context);
            ReflowChildren(_currentLayout, context);
            PostReflow(_currentLayout, context);
        }
        catch (Exception e)
        {
            Logger.Error("Exception during widget reflow: " + e.Message, e);
        }
        context.PopWidget();
    }
}