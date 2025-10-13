using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using User.Game.Actors;

namespace User.Game.Player.Components;

public partial class ExperienceComponent : ActorComponent
{
    public int Level { get; private set; } = 1;
    public double Experience { get; private set; } = 0.0;

    private double _expForPreviousLevel = 0;
    public double ExpForNextLevel = 100;
    
    public double CurrentPercentage => ExpForNextLevel < 1
        ? 1.0
        : Math.Clamp((Experience - _expForPreviousLevel) / (ExpForNextLevel - _expForPreviousLevel), 0.0, 1.0);

    [OnReady]
    protected void OnReady()
    {
        GainExperience(0);
    }
    
    [OnTimer(Seconds = 0.05f)]
    protected void CollectExperience()
    {
        foreach (var drop in ExperienceDrop.All.Where(enemy => enemy.WorldTransform.Position.DistanceSquaredTo(WorldTransform.Position) < 500))
        {
            drop.Collect();
            GainExperience(drop.ExperienceValue);
        }
    }

    private void GainExperience(double amount)
    {
        Experience += amount;
        if (ExpForNextLevel < 1)
            return;
        while (Experience >= ExpForNextLevel)
        {
            Level += 1;
            _expForPreviousLevel = ExpForNextLevel;
            ExpForNextLevel = Math.Round(100.0 * Math.Pow(Level, 1.5));
            Logger.Info("Level up!");
        }
        Logger.ShowPersistent(LogLevel.Info, "experience", $"Level: {Level} ({Experience}/{ExpForNextLevel} exp)");
    }
}