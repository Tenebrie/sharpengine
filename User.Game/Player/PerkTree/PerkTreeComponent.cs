using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Player.PerkTree;

public partial class PerkTreeComponent : ActorComponent
{
    public List<PlayerPerk> CurrentPerks = [];

    public PlayerPerk GetRandomOffering(PlayerPerk.PerkRarity rarity)
    {
        var random = new Random();
        List<Type> availablePerks = [typeof(TripleStatsPerk), typeof(PiercingBladesPerk), typeof(LightningAreaPerk), typeof(FanBladesPerk)];
        var perkType = availablePerks[random.Next(availablePerks.Count)];
        var perk = (PlayerPerk?)Activator.CreateInstance(perkType, rarity);
        if (perk == null)
            throw new Exception("Failed to create perk instance.");
        return perk;
    }
    
    public void AddPerk(PlayerPerk perk)
    {
        CurrentPerks.Add(perk);
        AdoptChild(perk);

        PlayerCharacter.All.First().Attributes.Recalculate();
    }
    
    [OnReady]
    protected void OnReady()
    {
        var playerCharacter = PlayerCharacter.All.First();
        playerCharacter.Experience.OnPrestigeLevel.Connect(this, OnPrestigeLevel);
    }

    private void OnPrestigeLevel(int newLevel)
    {
        AddPerk(new PrestigePerk(PlayerPerk.PerkRarity.Legendary));
    }
}