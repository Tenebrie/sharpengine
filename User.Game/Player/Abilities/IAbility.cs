namespace User.Game.Player.Abilities;

public interface IAbility
{
    public void OnCast();
    public void OnCooldownReduce(double deltaTime);
}