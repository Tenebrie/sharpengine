using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Engine.Core.Input.Contexts;
using Engine.Core.Logging;
using Silk.NET.Input;

namespace Engine.Module.Utility.Services;

[InputActions]
public enum InputAction
{
    MouseMove,
    MouseClick
}

public partial class LaminaInputService : Service
{
    private InputContext _baseContext = null!;

    [OnReady]
    protected void OnReady()
    {
        _baseContext = InputContext.GetBuilder<InputAction>()
            .Add(InputAction.MouseMove, MouseAxis.MoveX)
            .Add(InputAction.MouseMove, MouseAxis.MoveY)
            .Add(InputAction.MouseClick, MouseButton.Left)
            .Build();
        var activeContext = InputContext.From(_baseContext);
        GetService<InputService>().SetInputContext(this, activeContext);
    }
}