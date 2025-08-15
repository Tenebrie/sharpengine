using Engine.Core.Attributes;
using Engine.Core.Enum;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Silk.NET.Input;

namespace User.Game.Services;

[InputActions]
public enum InputAction
{
    MoveForward,
    MoveBackward,
    MoveLeft,
    MoveRight,
    Jump,
    Primary,
    Hotbar1,
    Hotbar2,
    Hotbar3,
    Hotbar4,
}

public partial class UserInputService : Service
{
    private InputContext _baseContext = null!;

    [OnReady]
    protected void OnReady()
    {
        _baseContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.MoveForward, Key.W)
            .Add(InputAction.MoveForward, Key.Up)
            .Add(InputAction.MoveBackward, Key.S)
            .Add(InputAction.MoveBackward, Key.Down)
            .Add(InputAction.MoveLeft, Key.A)
            .Add(InputAction.MoveLeft, Key.Left)
            .Add(InputAction.MoveRight, Key.D)
            .Add(InputAction.MoveRight, Key.Right)
            .Add(InputAction.Jump, Key.Space)
            .Add(InputAction.Primary, MouseButton.Left)
            .Add(InputAction.Hotbar1, Key.Number1)
            .Add(InputAction.Hotbar2, Key.Number2)
            .Add(InputAction.Hotbar3, Key.Number3)
            .Add(InputAction.Hotbar4, Key.Number4)
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

        var activeContext = InputContext.From(_baseContext);
        GetService<InputService>().InputContext = activeContext;
    }
}