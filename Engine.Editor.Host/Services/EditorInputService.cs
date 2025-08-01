using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Input;
using Engine.Input.Attributes;
using Engine.Input.Contexts;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;
using Silk.NET.Input;

namespace Engine.Editor.Host.Services;

[InputActions]
public enum InputAction
{
    // Base context
    HoldToControlCamera,
    
    // Camera control actions
    CameraForward,
    CameraBackward,
    CameraUp,
    CameraDown,
    CameraLeft,
    CameraRight,
    CameraRotatePitch,
    CameraRotateYaw,
    CameraSpeedWheel,
}

public partial class EditorInputService : Service
{
    private HostBackstage HostBackstage => (HostBackstage)Backstage;
    
    private InputContext _baseContext = null!;
    private InputContext _cameraControlContext = null!;
    
    [OnInit]
    protected void OnInit()
    {
        _baseContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.HoldToControlCamera, MouseButton.Right)
            .Build();
        
        _cameraControlContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.CameraForward, Key.W)
            .Add(InputAction.CameraForward, Key.Up)
            .Add(InputAction.CameraBackward, Key.S)
            .Add(InputAction.CameraBackward, Key.Down)
            .Add(InputAction.CameraUp, Key.E)
            .Add(InputAction.CameraDown, Key.Q)
            .Add(InputAction.CameraLeft, Key.A)
            .Add(InputAction.CameraLeft, Key.Left)
            .Add(InputAction.CameraRight, Key.D)
            .Add(InputAction.CameraRight, Key.Right)
            .Add(InputAction.CameraRotateYaw, MouseAxis.MoveX)
            .Add(InputAction.CameraRotatePitch, MouseAxis.MoveY)
            .Add(InputAction.CameraSpeedWheel, MouseAxis.WheelY)
            .Build();
        
        RecalculateActiveContext();
    }

    [OnKeyInput(Key.F5)]
    protected void OnToggleGameplayContext()
    {
        switch (HostBackstage.GameplayContext)
        {
            case GameplayContext.Editor:
                HostBackstage.Editor.SetGameplayContext(GameplayContext.EmbeddedPlay);
                break;
            case GameplayContext.EmbeddedPlay:
                HostBackstage.Editor.SetGameplayContext(GameplayContext.Editor);
                break;
            case GameplayContext.StandalonePlay:
            default:
                break;
        }

        RecalculateActiveContext();
    }
    
    private bool _isGameSuspended = false;
    [OnKeyInput(Key.F6)]
    protected void OnToggleGameSuspended()
    {
        _isGameSuspended = !_isGameSuspended;
        HostBackstage.Editor.SetGameplayTimeScale(_isGameSuspended ? 0.0 : 1.0);
    }
    
    [OnKeyInput(Key.F11)]
    protected void OnReload()
    {
        HostBackstage.Editor.ReloadUserGame();
    }
    
    [OnInput(InputAction.HoldToControlCamera)]
    [OnInputReleased(InputAction.HoldToControlCamera)]
    protected void RecalculateActiveContext()
    {
        if (HostBackstage.GameplayContext != GameplayContext.Editor)
        {
            GetService<InputService>().InputContext = InputContext.Empty;
            return;
        }
        
        var activeContext = InputContext.From(_baseContext);
        if (GetService<InputService>().IsInputHeld(InputAction.HoldToControlCamera))
        {
            activeContext = activeContext.Combine(_cameraControlContext);
        }
        
        GetService<InputService>().InputContext = activeContext;
    }
}