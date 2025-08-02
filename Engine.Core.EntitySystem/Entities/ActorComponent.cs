namespace Engine.Core.EntitySystem.Entities;

public partial class ActorComponent : Spatial
{
    public Actor Actor { get; set; } = null!;
}