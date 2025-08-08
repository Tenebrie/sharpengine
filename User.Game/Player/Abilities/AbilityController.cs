using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Player.Abilities.Definitions;
using User.Game.Services;

namespace User.Game.Player.Abilities;

public partial class AbilityController : ActorComponent
{
    [Component] public PiercingBladeAbility PiercingBlade;
    [Component] public LightningStrikeAbility LightningStrike;

    private IAbility? _currentAbility;
    private List<IAbility?> _hotbar = [];

    [OnReady]
    protected void OnReady()
    {
        _hotbar = [PiercingBlade, LightningStrike, null, null];
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        _currentAbility?.OnCooldownReduce(deltaTime);
    }
    
    [OnInput(InputAction.Hotbar1, 0)]
    [OnInput(InputAction.Hotbar2, 1)]
    [OnInput(InputAction.Hotbar3, 2)]
    [OnInput(InputAction.Hotbar4, 3)]
    protected void OnAbilitySwitch(int value)
    {
        _currentAbility = _hotbar[value];
    }
    
    [OnInput(InputAction.Primary)]
    [OnInputHeld(InputAction.Primary)]
    protected void OnCastPrimary()
    {
        _currentAbility?.OnCast();
    }
}