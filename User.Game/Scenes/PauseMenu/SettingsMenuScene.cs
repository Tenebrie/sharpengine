using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Engine.Core.Logging;
using Silk.NET.Input;
using SixLabors.ImageSharp.PixelFormats;
using User.Game.Services;

namespace User.Game.Scenes.PauseMenu;

[InputActions]
public enum InputActions
{
    TogglePauseMenu,
}

public partial class SettingsMenuScene : Scene
{
    [Component] protected UserInterfaceComponent Widget;
    
    [OnReady]
    protected void OnReady()
    {
        CreateActor<SettingsMenuControls>();
        Widget.Visible = false;
    }
    
    [OnInput(InputActions.TogglePauseMenu)]
    protected void OnTogglePauseMenu()
    {
        Widget.Visible = !Widget.Visible;
        GetService<UserInputService>().SetPauseMenuOpened(Widget.Visible);
        
        if (Widget.Visible)
        {
            FirstPersonScene.All.First().Pause();
            GetService<InputService>().SetMouseCursor(new CursorModifier(this)
            {
                Mode = CursorMode.Normal,
                Priority = 1
            });
        }
        else
        {
            FirstPersonScene.All.First().Unpause();
            GetService<InputService>().ClearMouseCursor(this);
        }
        
        Widget.SetLayout(v =>
        {
            Widget.Position = (-5000, -5000);
            GameThreadTask.NextFrame(() =>
            {  
                GameThreadTask.NextFrame(() =>
                {
                    Widget.Position = Backstage.Window.Size / 2 - Widget.InnerContentSize / 2;
                });
            });
            
            v.Box(v =>
            { 
                v.Image(tint: Color.FromArgb(100, 0, 255, (int)FrameCounter.Current % 2), borderRadius: 6);
                v.Flex(padding: (16, 8), gap: 8, children: v =>
                {
                    v.Label(text: "Paused", fontSize: 32, padding: (0, 0), color: Color.White);
                    v.Image(tint: Color.FromArgb(180, 0, 0, 0), size: (0, 1));
                    v.Button(onClick: () => Logger.Info("Button works!"), label: "Click me", backgroundColor: Color.WhiteSmoke);
                    v.Button(onClick: () => Logger.Info("Button works!"), label: "Click me", backgroundColor: Color.WhiteSmoke);
                    v.Button(onClick: () => Logger.Info("Button works!"), label: "Click me", backgroundColor: Color.WhiteSmoke);
                });
            });
        });
    }
}

public partial class SettingsMenuControls : Actor
{
    private InputContext _context;
    
    [OnReady]
    protected void OnReady()
    {
        _context = InputContext.GetBuilder<InputActions>()
            .Add(InputActions.TogglePauseMenu, Key.Escape)
            .Build();
        
        Get<InputService>().SetInputContext(this, _context);
    }
}