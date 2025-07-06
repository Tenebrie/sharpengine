using Engine.Core.Common;

namespace Engine.Worlds.Entities;


public abstract class Unit
{
    public Transform Transform { get; } = Transform.Identity;
    
    protected internal virtual void OnInit() {}
    protected internal virtual void OnUpdate(double deltaTime) {}
}