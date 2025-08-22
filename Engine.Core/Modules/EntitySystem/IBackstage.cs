using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Engine.Core.Modules.EntitySystem;

public interface IHostBackstage : IBackstage;

public interface IBackstage : IModularHost
{
    public string Name { get; set; }
    public void FreeImmediately();

    public void StartupInitialize();
    public void TriggerLogicFrameUpdate(double deltaTime);
}

public interface ISpatial;
public interface IPhysicsComponent;