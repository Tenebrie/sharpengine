using Engine.Core.Assets.Meshes;
using Engine.Core.Enum;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Silk.NET.Input;

namespace Engine.Module.Host.Services;

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

public enum DebugRenderingMode
{
    None,
    BoundingBoxes,
    Colliders,
    Count,
}

public partial class EditorInputService : Service
{
    private WorkspaceHost WorkspaceHost => (WorkspaceHost)Backstage;
    
    private InputContext _baseContext = null!;
    private InputContext _cameraControlContext = null!;
    
    [OnReady]
    protected void OnReady()
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
        NotifyDebugRendering();
    }

    private DebugRenderingMode _debugMode = DebugRenderingMode.None;
    [OnKeyInput(Key.F4)]
    protected void OnToggleDebugRendering()
    {
        _debugMode += 1;
        if (_debugMode >= DebugRenderingMode.Count)
            _debugMode = DebugRenderingMode.None;
        NotifyDebugRendering();
    }

    private void NotifyDebugRendering()
    {
        LineSphereMesh.VisibleModes = _debugMode switch
        {
            DebugRenderingMode.BoundingBoxes => LineSphereMesh.ColorMode.AxisColor,
            DebugRenderingMode.Colliders => LineSphereMesh.ColorMode.Collider,
            _ => LineSphereMesh.ColorMode.None
        };
    }

    [OnKeyInput(Key.F5)]
    protected void OnToggleGameplayContext()
    {
        switch (WorkspaceHost.GameplayContext)
        {
            case GameplayContext.Editor:
                WorkspaceHost.Hypervisor.SetGameplayContext(GameplayContext.EmbeddedPlay);
                break;
            case GameplayContext.EmbeddedPlay:
                WorkspaceHost.Hypervisor.SetGameplayContext(GameplayContext.Editor);
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
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, _isGameSuspended ? 0.0 : 1.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, _isGameSuspended ? 0.0 : 1.0);
    }
    
    [OnKeyInput(Key.F7, 1)]
    [OnKeyInputReleased(Key.F7, 0)]
    protected void OnToggleGameSuspended(int value)
    {
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, value == 1 ? 10.0 : _isGameSuspended ? 0.0 : 1.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, value == 1 ? 10.0 : _isGameSuspended ? 0.0 : 1.0);
    }
    
    [OnKeyInput(Key.F11)]
    protected void OnReload()
    {
        WorkspaceHost.Hypervisor.ReloadEngineModule(EngineModule.Gameplay);
    }
    
    private int _framesToAdvanceRemaining = 0;
    [OnKeyInput(Key.Period)]
    protected void OnAdvanceOneFrame()
    {
        _keyHeldAccumulator = 0;
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, 1.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, 1.0);
        _framesToAdvanceRemaining = 2;
    }
    
    [OnKeyInputReleased(Key.Period)]
    protected void OnStopAdvancingFrames()
    {
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, 0.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, 0.0);
        _framesToAdvanceRemaining = 0;
    }

    private double _keyHeldAccumulator = 0.0;
    [OnKeyInputHeld(Key.Period)]
    protected void OnHoldToAdvanceFrames(double deltaTime)
    {
        _keyHeldAccumulator += deltaTime;
        if (_keyHeldAccumulator < 0.15)
            return;
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, 1.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, 1.0);
        _framesToAdvanceRemaining = 2;
        _keyHeldAccumulator = 0;
    }

    [OnUpdate]
    protected void OnStopAfterOneFrame()
    {
        if (_framesToAdvanceRemaining == 0)
            return;
        _framesToAdvanceRemaining -= 1;
        if (_framesToAdvanceRemaining > 0)
            return;
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Gameplay, 0.0);
        WorkspaceHost.Hypervisor.SetTimeScale(EngineModule.Physics, 0.0);
    }
    
    [OnInput(InputAction.HoldToControlCamera)]
    [OnInputReleased(InputAction.HoldToControlCamera)]
    protected void RecalculateActiveContext()
    {
        if (WorkspaceHost.GameplayContext != GameplayContext.Editor)
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