using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;

namespace Engine.Core.EntitySystem.Components.Lamina;

public partial class WidgetComponent : Actor, IWidget
{
    private LaminaLayout? _currentLayout;
    
    public void SetLayout(LaminaLayout layout)
    {
        // TODO: Diff and update instead of clearing and recreating everything
        _currentLayout = layout;
        foreach (var child in Children)
            child.QueueFree();
        foreach (var childLayout in layout.Children)
        {
            if (!LaminaRendererRepository.TryGet(childLayout, out var renderer)) 
                throw new InvalidOperationException($"No renderer registered for layout of type {childLayout.GetType().Name}");
            var widget = (WidgetComponent?)Activator.CreateInstance(renderer.WidgetType);
            if (widget == null)
            {
                throw new Exception($"Failed to create instance of widget type {renderer.WidgetType.Name}");
            }

            AdoptChild(widget);
            widget.SetLayout(childLayout);
        }
    }

    protected virtual void Render(LaminaLayout layout, ILaminaRenderContext context) {}
    protected virtual void PostRender(LaminaLayout layout, ILaminaRenderContext context) {}
    public void PerformRender(ILaminaRenderContext context)
    {
        if (_currentLayout == null)
            return;
        
        Render(_currentLayout, context);
        foreach (var child in Children)
        {
            if (child is WidgetComponent widget)
                widget.PerformRender(context);
        }
        PostRender(_currentLayout, context);
    }
}