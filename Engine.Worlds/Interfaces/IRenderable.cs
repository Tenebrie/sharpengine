using Engine.Assets.Rendering;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Interfaces;

public interface IRenderable
{
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera);
    public int GetInstanceCount();
    public void PrepareRender(ref RenderContext renderContext);
    public void Render(ref RenderContext renderContext);
}
