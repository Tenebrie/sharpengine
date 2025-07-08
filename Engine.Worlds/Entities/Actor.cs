using Engine.Core.Common;

namespace Engine.Worlds.Entities;

public abstract class Actor : Atom
{
    public Transform Transform { get; } = Transform.Identity;
}