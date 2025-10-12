using Engine.Core.Common;

namespace Engine.Core.EntitySystem.Interfaces;

public interface ICullable : IRenderable
{
    // public bool CullingEnabled { get; }
    // public Vector3 BoundingSphereWorldOrigin { get; }
    // public double BoundingSphereWorldRadius { get; }
    public CullingRequest? ProduceCullingRequest();
}

public struct CullingRequest
{
    public required Vector3 Position;
    public required double BoundingSphereRadius;
}
