using System.Diagnostics.CodeAnalysis;
using Engine.Core.Attributes;
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
public partial class InputService : Service, IInputContextProvider
{
    private static InputHandler _inputHandler = null!;

    public GameplayContext RunsInGameplayContext { get; set; } = GameplayContext.StandalonePlay | GameplayContext.EmbeddedPlay;

    [OnCreate]
    protected void OnCreate()
    {
        _inputHandler ??= new InputHandler(Backstage.Hypervisor);
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
    public CursorModifier SetMouseCursor(CursorModifier modifier)
    {
        return _inputHandler.AddMouseCursorModifier(new CursorModifier(modifier)
        {
            Backstage = Backstage,
            Provider = this
        });
    }

    public void ClearMouseCursor(object ownerIdentity)
    {
        _inputHandler.RemoveMouseCursorModifier(ownerIdentity);
    }

    public void SetInputContext(object ownerIdentity, InputContext inputContext)
    {
        _inputHandler.SetInputContext(this, ownerIdentity, inputContext);
    }

    public void RemoveInputContext(object ownerIdentity)
    {
        _inputHandler.RemoveInputContext(ownerIdentity);
    }

    [OnGameplayContextChange]
    protected void OnGameplayContextChange() => _inputHandler.RecalculateMouseCursor();

    [OnDestroy]
    protected void OnDestroy() => _inputHandler.PurgeAll(this);

    public Dictionary<long, List<BoundHeldAction>> OnInputEvent => _inputHandler.OnInputEvent;
    public Dictionary<(IBackstage identity, long actionId), List<BoundHeldAction>> OnInputHeldEvent => _inputHandler.OnInputHeldEvent;
    public Dictionary<long, List<BoundHeldAction>> OnInputReleasedEvent => _inputHandler.OnInputReleasedEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyEvent => _inputHandler.OnKeyboardKeyEvent;
    public Dictionary<(IBackstage identity, Key key), List<BoundHeldAction>> OnKeyboardKeyHeldEvent => _inputHandler.OnKeyboardKeyHeldEvent;
    public Dictionary<Key, List<BoundHeldAction>> OnKeyboardKeyReleasedEvent => _inputHandler.OnKeyboardKeyReleasedEvent;
}