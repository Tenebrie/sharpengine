using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;

namespace User.Game.Player.Components;

public partial class ExperienceComponent : ActorComponent
{
    [Signal] public Signal<int> OnLevelUp;
    [Signal] public Signal<int> OnPrestigeLevel;
    
    public int Level { get; private set; } = 1;
    public int PrestigeLevel => Level > PrestigeAfter ? Level - PrestigeAfter : 0;
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
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        var player = PlayerCharacter.All.FirstOrDefault();
        if (player == null)
            return;
        if (Level < 2)
        {
            GainExperience(deltaTime * 150);
        }
        GainExperience(player.Attributes.ExperienceGain * deltaTime);
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

    private const int PrestigeAfter = 30;

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

            if (Level <= PrestigeAfter)
                OnLevelUp.Emit(Level);
            else
                OnPrestigeLevel.Emit(Level);
        }
    }
}