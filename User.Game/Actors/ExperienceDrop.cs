using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class ExperienceDrop : ActorInstance
{
    [DefaultGroup] public static readonly Group<ExperienceDrop> All = new(); 

    public double ExperienceValue { get; set; } = 0.0;

    public void Collect()
    {
        QueueFree();
    }
}