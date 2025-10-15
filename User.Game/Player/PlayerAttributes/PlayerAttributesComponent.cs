using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using User.Game.Actors.UserInterface;

namespace User.Game.Player.PlayerAttributes;

public enum AttributeType
{
    Power,
    Speed,
    Health,
    ExperienceGain,
    ActiveRechargeRate,
    PassiveRechargeRate,
    
    BladePierceCount,
    BladeExtraChance,
    LightningArea,
    HeadPats,
}

public partial class PlayerAttributesComponent : ActorComponent
{
    public readonly Dictionary<AttributeType, double> Attributes = new();
    
    public double Power => Attributes.TryGetValue(AttributeType.Power, out var value) ? value : 0;
    public double Speed => Attributes.TryGetValue(AttributeType.Speed, out var value) ? value : 0;
    public double Health => Attributes.TryGetValue(AttributeType.Health, out var value) ? value : 0;
    public double ExperienceGain => Attributes.TryGetValue(AttributeType.ExperienceGain, out var value) ? value : 0;
    public double ActiveRechargeRate => Attributes.TryGetValue(AttributeType.ActiveRechargeRate, out var value) ? value : 0;
    public double PassiveRechargeRate => Attributes.TryGetValue(AttributeType.PassiveRechargeRate, out var value) ? value : 0;
    
    public double Get(AttributeType type) => Attributes.TryGetValue(type, out var value) ? value : 0;

    public void Recalculate()
    {
        Attributes.Clear();
        var allAttributes = Enum.GetValues<AttributeType>();
        foreach (var attribute in allAttributes)
        {
            Attributes[attribute] = 0;
        }

        var player = GetParent<PlayerCharacter>();
        if (player == null)
            throw new Exception("PlayerAttributesComponent must be a child of PlayerCharacter.");
        var perkTree = player.PerkTree;

        foreach (var perk in perkTree.CurrentPerks)
        {
            foreach (var (attribute, boost) in perk.BoostedAttributes)
            {
                if (!Attributes.TryAdd(attribute, boost))
                {
                    Attributes[attribute] += boost;
                }
            }
        }
        
        Logger.Info("Current stats: ");
        foreach (var (attribute, value) in Attributes)
        {
            Logger.Info($" - {attribute}: {value}");
        }
    }
}