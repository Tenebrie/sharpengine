using Engine.Core.Common;

namespace Engine.Worlds.Entities;

public abstract class Spatial : Atom
{
    public Transform Transform { get; } = Transform.Identity;
    public Transform WorldTransform
    {
        get
        {
            if (Parent is Spatial parentActor)
                return Transform * parentActor.WorldTransform;
            return Transform;
        }
    }
}