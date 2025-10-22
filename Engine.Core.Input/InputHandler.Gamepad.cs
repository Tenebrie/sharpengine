using Engine.Core.Common;
using Engine.Core.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine.Core.Input;

public enum GamepadAxis
{
    X,
    Y,
}

public enum GamepadAnalog
{
    LeftThumbstickX,
    LeftThumbstickY,
    RightThumbstickX,
    RightThumbstickY,
    LeftTrigger,
    RightTrigger
}

public partial class InputHandler
{
    private List<IGamepad> KnownGamepads { get; } = [];
    
    private readonly HashSet<Button> _heldGamepadButtons = [];
    private readonly Vector2[] _lastThumbstickPositions = [Vector2.Zero, Vector2.Zero];
    
    public void BindGamepadEvents(IGamepad gamepad)
    {
        if (KnownGamepads.Contains(gamepad))
            return;
        
        // gamepad.
        // gamepad.MouseMove += OnMouseMove;
        // gamepad.MouseDown += OnMouseButtonDown;
        // gamepad.MouseUp += OnMouseButtonUp;
        // gamepad.Scroll += OnMouseScroll;
        gamepad.ButtonDown += OnGamepadButtonDown;
        gamepad.ButtonUp += OnGamepadButtonUp;
        gamepad.ThumbstickMoved += OnGamepadThumbstickMoved;
        KnownGamepads.Add(gamepad);
    }
    
    public void UnbindGamepadEvents()
    {
        foreach (var gamepad in KnownGamepads)
        {
            // gamepad.MouseMove -= OnMouseMove;
            // gamepad.MouseDown -= OnMouseButtonDown;
            // gamepad.MouseUp -= OnMouseButtonUp;
            // gamepad.Scroll -= OnMouseScroll;
            gamepad.ButtonDown -= OnGamepadButtonDown;
            gamepad.ButtonUp -= OnGamepadButtonUp;
            gamepad.ThumbstickMoved -= OnGamepadThumbstickMoved;
        }
        KnownGamepads.Clear();
    }
    
    public Vector2 GetGamepadAnalogPosition(GamepadAnalog analog)
    {
        return analog switch
        {
            GamepadAnalog.LeftThumbstickX => new Vector2(_lastThumbstickPositions[0].X, 0),
            GamepadAnalog.LeftThumbstickY => new Vector2(0, _lastThumbstickPositions[0].Y),
            GamepadAnalog.RightThumbstickX => new Vector2(_lastThumbstickPositions[1].X, 0),
            GamepadAnalog.RightThumbstickY => new Vector2(0, _lastThumbstickPositions[1].Y),
            _ => throw new ArgumentOutOfRangeException(nameof(analog), "Unsupported gamepad analog " + analog)
        };
    }
    
    public bool IsGamepadButtonHeld(ButtonName button)
    {
        return _heldGamepadButtons.Any(b => b.Name == button);
    }

    private void OnGamepadButtonDown(IGamepad mouse, Button button)
    {
        _heldGamepadButtons.Add(button);
        UserInputMode = UserInputMode.Gamepad;
        
        var triggeredActions = CurrentContext.Match(button);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInput: {e.Message}", e);
                }
            }
        });
    }
    
    private void OnGamepadButtonUp(IGamepad mouse, Button button)
    {
        _heldGamepadButtons.RemoveWhere(btn => btn.Name == button.Name);
        UserInputMode = UserInputMode.Gamepad;
        
        var triggeredActions = CurrentContext.Match(button);
        triggeredActions.ForEach(triggeredActionId =>
        {
            OnInputReleasedEvent.TryGetValue(triggeredActionId, out var inputActionList);
            if (inputActionList == null) return;
                
            foreach (var boundAction in inputActionList)
            {
                try 
                {
                    boundAction.Action.Invoke(boundAction.X, boundAction.Y, boundAction.Z, 0.0f);
                }
                catch (Exception e)
                {
                    Logger.Error($"Error in OnInputReleased: {e.Message}", e);
                }
            }
        });
    }
    
    private void OnGamepadThumbstickMoved(IGamepad gamepad, Thumbstick thumbstick)
    {
        UserInputMode = UserInputMode.Gamepad;
        
        var axisValues = new Vector2(thumbstick.X, thumbstick.Y);
        if (Math.Abs(thumbstick.X) <= 0.1)
            axisValues.X = 0.0f;
        if (Math.Abs(thumbstick.Y) <= 0.1)
            axisValues.Y = 0.0f;
        
        var previousPosition = _lastThumbstickPositions[thumbstick.Index];
        
        // Check for X axis first
        var analog = thumbstick.Index switch
        {
            0 => GamepadAnalog.LeftThumbstickX,
            1 => GamepadAnalog.RightThumbstickX,
            _ => throw new ArgumentOutOfRangeException(nameof(thumbstick), "Unsupported thumbstick index " + thumbstick.Index)
        };
        
        var wasEngaged = Math.Abs(previousPosition.X) > 0;
        var isNowEngaged = Math.Abs(axisValues.X) > 0;
        
        if (!wasEngaged && isNowEngaged)
            OnGamepadAnalogEngaged(analog, axisValues);
        else if (wasEngaged && !isNowEngaged)
            OnGamepadAnalogReleased(analog);
        
        // Then check for Y axis
        analog = thumbstick.Index switch
        {
            0 => GamepadAnalog.LeftThumbstickY,
            1 => GamepadAnalog.RightThumbstickY,
            _ => throw new ArgumentOutOfRangeException(nameof(thumbstick), "Unsupported thumbstick index " + thumbstick.Index)
        };
        wasEngaged = Math.Abs(previousPosition.Y) > 0;
        isNowEngaged = Math.Abs(axisValues.Y) > 0;
        if (!wasEngaged && isNowEngaged)
            OnGamepadAnalogEngaged(analog, axisValues);
        else if (wasEngaged && !isNowEngaged)
            OnGamepadAnalogReleased(analog);
        
        _lastThumbstickPositions[thumbstick.Index] = axisValues;
    }
    
    private void OnGamepadAnalogEngaged(GamepadAnalog analog, Vector2 axisValues)
    {
        var triggeredActions = CurrentContext.Match(analog);
        TriggerOnInputEngaged(triggeredActions, (axisValues.X, axisValues.Y, 0.0));
    }
    
    private void OnGamepadAnalogReleased(GamepadAnalog analog)
    {
        var triggeredActions = CurrentContext.Match(analog);
        TriggerOnInputReleased(triggeredActions, Vector3.One);
    }

    /// <summary>
    /// Process OnGamepadButtonHeld and OnInputHeld events for all currently held gamepad buttons and analogs.
    /// </summary>
    /// <returns>List of triggered input actions (by long representation of the bound enum)</returns>
    private void SendHeldGamepadButtonEvents(ref Dictionary<string, List<BoundHeldAction>> triggeredHandlers)
    {
        foreach (var heldButton in _heldGamepadButtons)
        {
            var triggeredActions = CurrentContext.Match(heldButton);
            TriggerOnInputHeld(triggeredActions, Vector3.One, ref triggeredHandlers);
        }
        for (var i = 0; i < _lastThumbstickPositions.Length; i++)
        {
            var position = _lastThumbstickPositions[i];
            if (Math.Abs(position.X) > 0.1)
            {
                var analog = i switch
                {
                    0 => GamepadAnalog.LeftThumbstickX,
                    1 => GamepadAnalog.RightThumbstickX,
                    _ => throw new Exception("Unsupported thumbstick index " + i)
                };
                var triggeredActions = CurrentContext.Match(analog);
                TriggerOnInputHeld(triggeredActions, (position.X, position.X, position.X), ref triggeredHandlers);
            }

            if (Math.Abs(position.Y) > 0.1)
            {
                var analog = i switch
                {
                    0 => GamepadAnalog.LeftThumbstickY,
                    1 => GamepadAnalog.RightThumbstickY,
                    _ => throw new Exception("Unsupported thumbstick index " + i)
                };
                var triggeredActions = CurrentContext.Match(analog);
                TriggerOnInputHeld(triggeredActions, (-position.Y, -position.Y, -position.Y), ref triggeredHandlers);
            }
        }
    }
}