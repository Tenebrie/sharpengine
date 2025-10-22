using System.Diagnostics.CodeAnalysis;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Enum;
using Engine.Core.Input;
using Engine.Core.Input.Contexts;
using Engine.Core.Modules.EntitySystem;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Core.EntitySystem.Services;

[PublicAPI]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract")]
public partial class InputService : Service
{
    private static InputHandler _inputHandler = null!;
    
    private GameplayContext _runsInGameplayContext = GameplayContext.StandalonePlay | GameplayContext.EmbeddedPlay;

    public GameplayContext RunsInGameplayContext
    {
        get => _runsInGameplayContext;
        set
        {
            _runsInGameplayContext = value;
            _inputHandler.Register(this, _inputContext, value);
        }
    }

    private InputContext _inputContext = InputContext.Empty;
    public InputContext InputContext
    {
        get => _inputContext;
        set
        {
            _inputContext = value;
            _inputHandler.Register(this, value, _runsInGameplayContext);
        }
    }

    [OnCreate]
    protected void OnCreate()
    {
        _inputHandler ??= new InputHandler(Backstage.Hypervisor);
        _inputHandler.Register(this, _inputContext, RunsInGameplayContext);
    }
    
    public bool IsInputHeld<TInputActionEnum>(TInputActionEnum inputAction) where TInputActionEnum : System.Enum
    {
        if (inputAction.GetType().IsEnum)
            return _inputHandler.IsInputHeld(EnumBaseId.GetFor(typeof(TInputActionEnum)) + Convert.ToInt64(inputAction));
        
        throw new ArgumentException($"Input action must be a long or an enum type, but was {inputAction.GetType().Name}");
    }
    public bool IsKeyHeld(Key key) => _inputHandler.IsKeyHeld(key);
    public bool IsMouseButtonHeld(MouseButton button) => _inputHandler.IsMouseButtonHeld(button);
    public bool IsGamepadButtonHeld(ButtonName button) => _inputHandler.IsGamepadButtonHeld(button);
    public void BindMouseEvents(IMouse mouse) => _inputHandler.BindMouseEvents(mouse);
    public void BindGamepadEvents(IGamepad gamepad) => _inputHandler.BindGamepadEvents(gamepad);
    public void BindKeyboardEvents(IKeyboard keyboard) => _inputHandler.BindKeyboardEvents(keyboard);
    public void SendKeyboardHeldEvents(double deltaTime) => _inputHandler.SendHeldInputEvents(Backstage, deltaTime);
    public void ClearSubscriptions(Atom owner) => _inputHandler.ClearSubscriptions(owner);
    public UserInputMode UserInputMode => _inputHandler.UserInputMode;
    
    public Vector2 GetMousePosition() => _inputHandler.GetMousePosition();
    public Vector2 GetGamepadAnalogPosition(GamepadAnalog analog) => _inputHandler.GetGamepadAnalogPosition(analog);
    public void SetMousePosition(Vector2 position) => _inputHandler.SetMousePosition(position);
    public void SetMouseCursor(StandardCursor cursor) => _inputHandler.SetMouseCursor(cursor);
    public void SetMouseCursorMode(CursorMode mode) => _inputHandler.SetMouseCursorMode(mode);

    [OnDestroy]
    protected void OnDestroy() => _inputHandler.Unregister(this);

    public Dictionary<long, List<BoundHeldAction>> OnInputEvent => _inputHandler.OnInputEvent;
    public Dictionary<(IBackstage identity, long actionId), List<BoundHeldAction>> OnInputHeldEvent => _inputHandler.OnInputHeldEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputReleasedEvent => _inputHandler.OnInputReleasedEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent => _inputHandler.OnKeyboardKeyEvent;
    public Dictionary<(IBackstage identity, Key key), List<BoundHeldAction>> OnKeyboardKeyHeldEvent => _inputHandler.OnKeyboardKeyHeldEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent => _inputHandler.OnKeyboardKeyReleasedEvent;
}