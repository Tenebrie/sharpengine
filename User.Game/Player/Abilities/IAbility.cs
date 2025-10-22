using Engine.Core.Common;

namespace User.Game.Player.Abilities;

public interface IAbility
{
    public bool Ready { get; }
    public void OnCast();
    public void OnAutoCast(Vector3 targetPoint);
    public void OnCooldownReduce(double deltaTime);
    public double GetCooldownProgress();
}