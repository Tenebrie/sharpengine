using Engine.Worlds.Entities;

namespace Engine.Worlds.Interfaces;

public interface IRenderable
{
    public bool IsOnScreen { get; set; }
    public void PerformCulling(Camera activeCamera);
    public void Render();
}