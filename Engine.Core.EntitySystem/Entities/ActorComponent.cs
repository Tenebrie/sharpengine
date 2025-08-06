namespace Engine.Core.EntitySystem.Entities;

public partial class ActorComponent : Actor
{
    public Actor Actor { get; set; } = null!;
}