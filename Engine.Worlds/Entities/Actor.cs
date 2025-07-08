using Engine.Core.Common;

namespace Engine.Worlds.Entities;

public abstract class Actor : Atom
{
    // public List<Atom> Components { get; set; } = [];
    public Transform Transform { get; } = Transform.Identity;
}