using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace User.Game.Services;

public partial class UserInterfaceService : Service
{
    [Component] 
    protected UserInterfaceComponent UserInterface;

    protected int _counter = 0;

    [OnTimer(Seconds = 1)]
    protected void OnTimer()
    {
        Logger.Info("TICK ");
        UserInterface.SetLayout(v =>
        {
            v.Div(new Vector2(0, 200), v =>
            {
                v.Label("With some imagination, that's a UI framework");
            });
            v.Div(new Vector2(400 + _counter * 10, _counter * 10), v => 
            {
                v.Label("Hello world: " + _counter++);
            }); 
        });
    }
    //
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