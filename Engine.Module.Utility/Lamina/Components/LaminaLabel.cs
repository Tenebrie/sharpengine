using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class LaminaLayoutAttribute(string Alias) : Attribute;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class LaminaRendererAttribute : Attribute;

[UsedImplicitly]
public partial class LaminaLabel : WidgetComponent
{
    [LaminaRenderer]
    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not LabelLayout labelLayout)
            throw new ArgumentException($"Expected layout of type {nameof(LabelLayout)}, got {layout.GetType().Name}");
        context.RenderText("RobotoMono-Bold", 64, labelLayout.Text, new Vector2(0, 0), Color.White);
    }

}
