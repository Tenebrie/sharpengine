using Engine.Core.Assets.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Interfaces;

public interface IRenderable
{
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera);
    public int GetInstanceCount();
    public void PrepareRender(ref RenderContext renderContext);
    public void Render(ref RenderContext renderContext);
}
