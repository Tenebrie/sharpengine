using System.Drawing;
using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Player.Abilities;
using User.Game.Services;

namespace User.Game.FirstPerson;

public partial class FirstPersonAbilityController : ActorComponent
{
    [DefaultGroup] public static readonly Group<FirstPersonAbilityController> All = new();
    
    [Component] public AbilityHotbar AbilityHotbar;
    [Component] public FirstPersonAttackAbility BladeAttackA;
    [Component] public FirstPersonAttackAbility BladeAttackB;
    [Component] public FirstPersonAttackAbility BladeAttackC;
    [Component] public FirstPersonClearAbility BladeClear;

    private IAbility? _currentAbility;
    private List<IAbility?> _hotbar = [];
    
    public int CurrentAbilityIndex => _hotbar.IndexOf(_currentAbility);
    
    public double GetProgress(int index)
    {
        var ability = _hotbar[index];
        if (ability == null)
            return 0.0;
        return ability.GetCooldownProgress();
    }

    [OnReady]
    protected void OnReady()
    {
        _hotbar = [BladeAttackA, BladeAttackB, BladeAttackC, BladeClear, null, null];
        _currentAbility = _hotbar[0];
        BladeAttackA.ProjectileTint = Color.DarkRed;
        BladeAttackB.ProjectileTint = Color.DarkGreen;
        BladeAttackC.ProjectileTint = Color.DarkBlue;
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        var parent = GetParent<Spatial>();
        if (parent == null)
            return;
        // if (!BasicEnemy.Alive.Any())
            // return;

        var player = FirstPersonPlayer.All.First();
        for (var index = 0; index < 4; index++)
        {
            var ability = _hotbar[index];
            ability?.OnCooldownReduce(deltaTime + deltaTime * (player.Attributes.ActiveRechargeRate * 0.1));
        }
        for (var index = 4; index < _hotbar.Count; index++)
        {
            var ability = _hotbar[index];
            ability?.OnCooldownReduce((deltaTime + deltaTime * (player.Attributes.PassiveRechargeRate * 0.1)) / 3);
        }

        // for (var index = 3; index < _hotbar.Count; index++)
        // {
        //     var ability = _hotbar[index];
        //     if (ability is not { Ready: true })
        //         continue;
        //     var closestEnemy = BasicEnemy.Alive
        //         .Where(enemy => enemy.WorldTransform.Position.DistanceTo(parent.WorldTransform.Position) < 100)
        //         .OrderBy(enemy => enemy.WorldTransform.Position.DistanceTo(parent.WorldTransform.Position))
        //         .FirstOrDefault();
        //     if (closestEnemy == null)
        //         continue;
        //     ability.OnAutoCast(closestEnemy.WorldTransform.Position);
        // }
    }
    
    [OnInput(InputAction.Hotbar1, 0)]
    [OnInput(InputAction.Hotbar2, 1)]
    [OnInput(InputAction.Hotbar3, 2)]
    [OnInput(InputAction.Hotbar4, 3)]
    protected void OnAbilitySwitch(int value)
    {
        _currentAbility = _hotbar[value];
    }
    
    [OnInput(InputAction.NextHotbar, 1.0)]
    [OnInput(InputAction.PreviousHotbar, -1.0)]
    protected void OnAbilityCycle(double doubleDir)
    {
        var direction = (int)doubleDir;
        if (_hotbar.Count == 0)
            return;
        
        var currentIndex = _hotbar.IndexOf(_currentAbility);
        var newIndex = (currentIndex + direction) % 4;
        if (newIndex < 0)
            newIndex += _hotbar.Count;
        _currentAbility = _hotbar[newIndex];
    }
    
    [OnInput(InputAction.Primary)]
    [OnInputHeld(InputAction.Primary)]
    protected void OnCastPrimary()
    {
        _currentAbility?.OnCast();
    }
}