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
    Shoot
}

public partial class UserInputService : Service
{
    private InputContext _baseContext;
    
    [OnInit]
    protected void OnInit()
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
            .Add(InputAction.Shoot, MouseButton.Left)
            .Build();
        
        RecalculateActiveContext();
    }
    
    [OnGameplayContextChanged]
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