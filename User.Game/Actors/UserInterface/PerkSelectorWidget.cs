using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Extensions;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Engine.Core.Lamina;
using User.Game.Player;
using User.Game.Player.PerkTree;
using User.Game.Player.PlayerAttributes;
using User.Game.Services;

namespace User.Game.Actors.UserInterface;

public partial class PerkSelectorWidget : Actor
{
    private PlayerCharacter _playerCharacter;
    
    private readonly List<PerkWidget> _perkWidgets = [];
    
    [OnReady]
    protected void OnReady()
    {
        var playerCharacter = PlayerCharacter.All.FirstOrDefault();
        if (playerCharacter == null)
            return;
        _playerCharacter = playerCharacter;
        _playerCharacter.Experience.OnLevelUp.Connect(this, OnPlayerLevelUp);
        
        for (var i = 0; i < 3; i++)
        {
            var perkWidget = CreateComponent<PerkWidget>();
            _perkWidgets.Add(perkWidget);
        }
    }
    
    private void OnPlayerLevelUp(int newLevel)
    {
        List<PlayerPerk.PerkRarity> rarityTable = [];
        for (var i = 0; i < 7; i++)
            rarityTable.Add(PlayerPerk.PerkRarity.Common);
        for (var i = 0; i < 5; i++)
            rarityTable.Add(PlayerPerk.PerkRarity.Uncommon);
        for (var i = 0; i < 8; i++)
            rarityTable.Add(PlayerPerk.PerkRarity.Rare);
        for (var i = 0; i < 10; i++)
            rarityTable.Add(PlayerPerk.PerkRarity.Epic);
        for (var i = 0; i < 100; i++)
            rarityTable.Add(PlayerPerk.PerkRarity.Legendary);

        var maxValue = newLevel + 10;
        
        _displayedPerks.Clear();
        UserScene.All.First().Pause();
        GetService<UserInputService>().SetSelectingPerkMode(true);
        for (var index = 0; index < _perkWidgets.Count; index++)
        {
            var widget = _perkWidgets[index];
            var val = Random.Shared.Next(newLevel, maxValue + 1);
            var rarity = rarityTable[val];
            var perk = _playerCharacter.PerkTree.GetRandomOffering(rarity);
            widget.DisplayPerk(perk, index - 1, keyToPick: (index + 1).ToString());
            _displayedPerks.Add(perk);
        }
    }
    
    private readonly List<PlayerPerk> _displayedPerks = [];
    
    [OnInput(InputAction.SelectPerk1, 0)]
    [OnInput(InputAction.SelectPerk2, 1)]
    [OnInput(InputAction.SelectPerk3, 2)]
    protected void OnSelectPerk(int perkIndex)
    {
        if (perkIndex < 0 || perkIndex >= _displayedPerks.Count)
            return;
        
        _playerCharacter.PerkTree.AddPerk(_displayedPerks[perkIndex]);
        foreach (var widget in _perkWidgets)
        {
            widget.Hide();
        } 

        UserScene.All.First().Unpause();
        GetService<UserInputService>().SetSelectingPerkMode(false);
    }
}

public partial class PerkWidget : ActorComponent
{
    [Component] private UserInterfaceComponent _userInterface;

    [OnReady]
    public void OnReady()
    {
        _userInterface.Visible = false;
    }
    
    public void DisplayPerk(PlayerPerk perk, int index, string keyToPick)
    {
        _userInterface.Visible = true;
        
        _userInterface.SetLayout(v =>
        {
            var windowSize = Backstage.Window.FramebufferSize;
            var windowCenterRaw = windowSize / 2;
            var windowCenter = new Vector2(windowCenterRaw.X, windowCenterRaw.Y);
        
            _userInterface.Size = (windowSize.X, windowSize.Y);
            if (GetService<InputService>().UserInputMode == UserInputMode.Gamepad)
            {
                keyToPick = index switch
                {
                    -1 => "X",
                    +0 => "Y",
                    +1 => "B",
                    _ => keyToPick
                };
            }
            
            Vector2 perkSize = (300, 600);
            const int perkSpacing = 50;
            var offset = new Vector2(perkSpacing + perkSize.X, 0) * index;
            v.Div(
                position: windowCenter - perkSize / 2 + offset, 
                children: v =>     
            {
                var rarityColor = perk.Rarity switch
                {
                    PlayerPerk.PerkRarity.Common => Color.FromArgb(255, 80, 80, 80),
                    PlayerPerk.PerkRarity.Uncommon => Color.DarkGreen,
                    PlayerPerk.PerkRarity.Rare => Color.DarkBlue,
                    PlayerPerk.PerkRarity.Epic => Color.Purple,
                    PlayerPerk.PerkRarity.Legendary => Color.DarkGoldenrod,
                    _ => Color.White
                };
                
                v.Image(position: (-5, -5), size: perkSize + (10, 10), tint: rarityColor); 
                v.Image(size: perkSize, tint: Color.FromArgb(255, 40, 20,20));
                v.Label(text: perk.Name, position: (10, 5), fontSize: 28, color: Color.White);
                v.Label(text: perk.Rarity.ToString(), position: (10, 40), fontSize: 20, color: Color.White);
                v.Label(text: perk.Description, position: (10, 80), color: Color.White); 
                
                v.Div(gap: 20, position: (10, 150), children: v =>
                {
                    foreach (var (attribute, value) in perk.BoostedAttributes)
                    {
                        var statColor = attribute switch
                        {
                            AttributeType.Power => Color.Red,
                            AttributeType.Speed => Color.Green,
                            AttributeType.Health => Color.Blue,  
                            AttributeType.ExperienceGain => Color.Gold,
                            AttributeType.ActiveRechargeRate => Color.Cyan,
                            AttributeType.PassiveRechargeRate => Color.Magenta,
                            _ => Color.White
                        };
                        v.Label(text: $"+{value} {attribute}", color: statColor);
                    }
                });
                
                v.Label(text: $"Press {keyToPick} to pick",
                    position: (10, perkSize.Y - 40),
                    color: Color.White,
                    fontSize: 30);
            });
        });
    }
    
    public void Hide()
    {
        _userInterface.Visible = false;
    }
}