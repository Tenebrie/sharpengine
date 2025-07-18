using Engine.Codegen.Bgfx.Unsafe;
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
}

public class EditorInputService : Service
{
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
            .Build();
        
        RecalculateActiveContext();
    }

    [OnInput(InputAction.HoldToControlCamera)]
    [OnInputReleased(InputAction.HoldToControlCamera)]
    protected void RecalculateActiveContext()
    {
        var activeContext = InputContext.From(_baseContext);
        Console.WriteLine(GetService<InputService>().IsInputHeld(InputAction.HoldToControlCamera));
        if (GetService<InputService>().IsInputHeld(InputAction.HoldToControlCamera))
        {
            activeContext = activeContext.Combine(_cameraControlContext);
        }
        
        GetService<InputService>().InputContext = activeContext;
    }

    [OnKeyInput(Key.F3)]
    protected void OnToggleRendererDebug()
    {
        var backstage = (HostBackstage)Backstage;
        backstage.Renderer?.ToggleDebugFlags(Bgfx.DebugFlags.Stats | Bgfx.DebugFlags.Profiler);
    }
}