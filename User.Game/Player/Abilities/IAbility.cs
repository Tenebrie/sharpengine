using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Player.Abilities;

public interface IAbility
{
    public bool Ready { get; }
    public void OnCast();
    public void OnAutoCast(Vector3 targetPoint);
    public void OnCooldownReduce(double deltaTime);
    public double GetCooldownProgress();
    public string GetIconPath();
}

public abstract partial class BaseAbility : ActorComponent, IAbility
{
    public abstract bool Ready { get; }
    public abstract void OnCast();
    public abstract void OnAutoCast(Vector3 targetPoint);
    public abstract void OnCooldownReduce(double deltaTime);
    public abstract double GetCooldownProgress();
    
    public string IconPath { get; set; } = "Textures/Icons/Placeholder.png";
    public string GetIconPath() => IconPath;
}
