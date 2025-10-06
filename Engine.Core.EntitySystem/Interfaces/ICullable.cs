using Engine.Core.Common;

namespace Engine.Core.EntitySystem.Interfaces;

public interface ICullable : IRenderable
{
    public bool CullingEnabled { get; }
    public Vector3 BoundingSphereWorldOrigin { get; }
    public double BoundingSphereWorldRadius { get; }
}
