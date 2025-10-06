using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Logging;
using Engine.Core.Profiling.Attributes;
using Silk.NET.Input;

namespace User.Game.Services;

public partial class UserInterfaceService : Service
{
    [Component] 
    protected UserInterfaceComponent UserInterface;

    protected int _counter = 0;
    protected Vector2 _lastMouse = Vector2.Zero;

    [OnUpdate]
    protected void OnTimer()
    {
        var mouse = GetService<InputService>().GetMousePosition();
        if (mouse == _lastMouse)
            return;
        
        _lastMouse = mouse;
        if (GetService<InputService>().IsMouseButtonHeld(MouseButton.Left))
        {
            Render(mouse);
        }
    }
    
    [OnReady]
    protected void Start()
    {
        Render(new Vector2(500, 8));
    }

    private void Render(Vector2 mouse)
    {
        UserInterface.SetLayout(v =>
        {
            v.Div(mouse, v =>
            {
                v.Button("Click me", () => _counter++);
            });
        });
    }
    
    // [OnReady]
    // protected void OnReady()
    // {
    //     UserInterface.SetLayout(v =>
    //     {
    //         v.Div(new Vector2(0, 200), v => 
    //         {
    //             v.Label("With some imagination, that's a UI framework");
    //         });
    //         v.Div(new Vector2(400, 0), v => 
    //         {
    //             v.Label("Hello world");
    //         });
    //     });
    // }
}