using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Module.Utility.Services;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class LaminaLayoutAttribute(string Alias) : Attribute;

[UsedImplicitly]
public partial class LaminaLabel : WidgetComponent
{
    // [OnUpdate]
    // public void OnUpdate()
    // {
    //     if (_currentLayout is LabelLayout label)
    //     {
    //         Logger.Info(label.Text);
    //     }
    // }
    
    [LaminaRenderer]
    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not LabelLayout labelLayout)
            throw new ArgumentException($"Expected layout of type {nameof(LabelLayout)}, got {layout.GetType().Name}");
        context.RenderText(labelLayout.Props.Font,
            labelLayout.Props.FontSize,
            labelLayout.Props.Text,
            labelLayout.Props.Position,
            labelLayout.Props.Color,
            2);
    }
}
