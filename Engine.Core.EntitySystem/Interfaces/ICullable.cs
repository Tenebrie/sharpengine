using Engine.Core.Common;

namespace Engine.Core.EntitySystem.Interfaces;

public interface ICullable : IRenderable
{
    public Vector3 BoundingSphereWorldOrigin { get; }
    public double BoundingSphereWorldRadius { get; }
}
