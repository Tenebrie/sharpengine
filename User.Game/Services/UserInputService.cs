using Engine.Core.Attributes;
using Engine.Core.Enum;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input;
using Engine.Core.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace User.Game.Services;

[InputActions]
public enum InputAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    MoveStick,
    Jump,
    Primary,
    Hotbar1,
    Hotbar2,
    Hotbar3,
    Hotbar4,
    SelectPerk1,
    SelectPerk2,
    SelectPerk3,
}

public partial class UserInputService : Service
{
    private InputContext _baseContext = null!;
    private InputContext _perkSelectorContext = null!;

    [OnReady]
    protected void OnReady()
    {
        _baseContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.MoveForward, Key.W)
            .Add(InputAction.MoveForward, Key.W, [KeyModifiers.Shift])
            .Add(InputAction.MoveForward, Key.Up)
            .Add(InputAction.MoveForward, ButtonName.DPadUp)
            .Add(InputAction.MoveForward, GamepadAnalog.LeftThumbstickY)
            .Add(InputAction.MoveBackward, Key.S)
            .Add(InputAction.MoveBackward, Key.S, [KeyModifiers.Shift])
            .Add(InputAction.MoveBackward, Key.Down)
            .Add(InputAction.MoveBackward, ButtonName.DPadDown)
            .Add(InputAction.MoveLeft, Key.A)
            .Add(InputAction.MoveLeft, Key.A, [KeyModifiers.Shift])
            .Add(InputAction.MoveLeft, Key.Left)
            .Add(InputAction.MoveLeft, ButtonName.DPadLeft)
            .Add(InputAction.MoveRight, Key.D)
            .Add(InputAction.MoveRight, Key.D, [KeyModifiers.Shift])
            .Add(InputAction.MoveRight, Key.Right)
            .Add(InputAction.MoveRight, ButtonName.DPadRight)
            .Add(InputAction.MoveRight, GamepadAnalog.LeftThumbstickX)
            .Add(InputAction.Jump, Key.Space)
            .Add(InputAction.Primary, MouseButton.Left)
            .Add(InputAction.Primary, MouseButton.Left, [KeyModifiers.Shift])
            .Add(InputAction.Primary, ButtonName.A)
            .Add(InputAction.Hotbar1, Key.Number1)
            .Add(InputAction.Hotbar1, ButtonName.LeftBumper)
            .Add(InputAction.Hotbar2, Key.Number2)
            .Add(InputAction.Hotbar2, ButtonName.RightBumper)
            .Add(InputAction.Hotbar3, Key.Number3)
            .Add(InputAction.Hotbar4, Key.Number4)
            .Build();
        
        _perkSelectorContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.SelectPerk1, Key.Number1)
            .Add(InputAction.SelectPerk1, ButtonName.X)
            .Add(InputAction.SelectPerk2, Key.Number2)
            .Add(InputAction.SelectPerk2, ButtonName.Y)
            .Add(InputAction.SelectPerk3, Key.Number3)
            .Add(InputAction.SelectPerk3, ButtonName.B)
            .Build();

        RecalculateActiveContext();
    }

    [OnGameplayContextChange]
    protected void RecalculateActiveContext()
    {
        if (Backstage.GameplayContext == GameplayContext.Editor)
        {
            GetService<InputService>().InputContext = InputContext.Empty;
            return;
        }

        if (_selectingPerkMode)
        {
            GetService<InputService>().InputContext = _perkSelectorContext;
            return;
        }
        var activeContext = InputContext.From(_baseContext);
        GetService<InputService>().InputContext = activeContext;
    }
    
    private bool _selectingPerkMode = false;
    public void SetSelectingPerkMode(bool enabled)
    {
        _selectingPerkMode = enabled;
        RecalculateActiveContext();
    }
}