using Engine.Core.EntitySystem.Entities;
using User.Game.Player.PlayerAttributes;

namespace User.Game.Player.PerkTree;

public abstract partial class PlayerPerk : ActorComponent
{
    public enum PerkRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public readonly Dictionary<AttributeType, int> BoostedAttributes = new();

    public string Name;
    public string Description;
    public PerkRarity Rarity;

    public PlayerPerk(string name, string description, PerkRarity rarity)
    {
        Name = name;
        Description = description;
        Rarity = rarity;
        List<AttributeType> allAttributes = [AttributeType.Power, AttributeType.Speed,
            AttributeType.ExperienceGain, AttributeType.ActiveRechargeRate, AttributeType.PassiveRechargeRate];
        var random = new Random();
        
        var numberOfAttributesToBoost = rarity switch
        {
            PerkRarity.Common => 1,
            PerkRarity.Uncommon => 2,
            PerkRarity.Rare => 2,
            PerkRarity.Epic => 3,
            PerkRarity.Legendary => 5,
            _ => 1
        };
        var attributeBoostMultiplier = rarity switch
        {
            PerkRarity.Common => 1.0,
            PerkRarity.Uncommon => 1.0,
            PerkRarity.Rare => 2.0,
            PerkRarity.Epic => 2.0,
            PerkRarity.Legendary => 3.0,
            _ => 1
        };

        for (var i = 0; i < numberOfAttributesToBoost; i++)
        {
            var randomAttribute = allAttributes[random.Next(allAttributes.Count)];
            if (BoostedAttributes.ContainsKey(randomAttribute))
                BoostedAttributes[randomAttribute] += (int)attributeBoostMultiplier;
            else
                BoostedAttributes[randomAttribute] = (int)attributeBoostMultiplier;
        }
    }
}

public partial class TripleStatsPerk : PlayerPerk
{
    public TripleStatsPerk(PerkRarity rarity): base("Exercise Plan", "Provides additional attributes", rarity)
    {
        foreach (var key in BoostedAttributes.Keys.ToList())
        {
            BoostedAttributes[key] *= 3;
        }
    }
}

public partial class PiercingBladesPerk : PlayerPerk
{
    public PiercingBladesPerk(PerkRarity rarity) : base("Piercing Blades", "Your blades now pierce through one\nadditional enemy per level.", rarity)
    {
        var boostValue = rarity switch
        {
            PerkRarity.Common => 1,
            PerkRarity.Uncommon => 1,
            PerkRarity.Rare => 2,
            PerkRarity.Epic => 2,
            PerkRarity.Legendary => 3,
            _ => 1
        };
        BoostedAttributes[AttributeType.BladePierceCount] = boostValue; 
    }
}

public partial class FanBladesPerk : PlayerPerk
{
    public FanBladesPerk(PlayerPerk.PerkRarity rarity) : base("Fan Blades",
        "You get +10% chance per level to toss\nan additional blade on each\ncast.",
        rarity)
    {
        var boostValue = rarity switch
        {
            PerkRarity.Common => 1,
            PerkRarity.Uncommon => 2,
            PerkRarity.Rare => 3,
            PerkRarity.Epic => 5,
            PerkRarity.Legendary => 10,
            _ => 1
        };
        BoostedAttributes[AttributeType.BladeExtraChance] = boostValue; 
    }
}

public partial class LightningAreaPerk : PlayerPerk
{
    public LightningAreaPerk(PerkRarity rarity) : base("Larger Lightning",
        "Your lightning impact area\nis now larger.\n+10% per level.",
        rarity)
    {
        var boostValue = rarity switch
        {
            PerkRarity.Common => 1,
            PerkRarity.Uncommon => 2,
            PerkRarity.Rare => 3,
            PerkRarity.Epic => 5,
            PerkRarity.Legendary => 10,
            _ => 1
        };
        BoostedAttributes[AttributeType.LightningArea] = boostValue;
    }
}

public partial class FallbackPerk : PlayerPerk
{
    public FallbackPerk(PerkRarity rarity) : base("Head Pats",
        "Very important.", rarity)
    {
        var boostValue = rarity switch
        {
            PerkRarity.Common => 1,
            PerkRarity.Uncommon => 2,
            PerkRarity.Rare => 3,
            PerkRarity.Epic => 5,
            PerkRarity.Legendary => 10,
            _ => 1
        };
        BoostedAttributes[AttributeType.HeadPats] = boostValue;
    }
}

public partial class PrestigePerk : PlayerPerk
{
    public PrestigePerk(PerkRarity rarity) : base("Prestige!", "", rarity)
    {
        BoostedAttributes[AttributeType.Power] = 5;
        BoostedAttributes[AttributeType.Speed] = 5;
        BoostedAttributes[AttributeType.Health] = 5;
        BoostedAttributes[AttributeType.ExperienceGain] = 1;
        BoostedAttributes[AttributeType.ActiveRechargeRate] = 5;
        BoostedAttributes[AttributeType.PassiveRechargeRate] = 5;
    }
}