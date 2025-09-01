using Engine.Core.Common;

namespace Engine.Core.Modules.EntitySystem;

public interface IHostBackstage : IBackstage;

public interface IBackstage : IModularHost
{
    public string Name { get; set; }
    public void FreeImmediately();

    public void StartupInitialize();
    public void TriggerLogicFrameUpdate(double deltaTime);
}

public interface IAtom;

public interface ISpatial
{
    public Transform Transform { get; set; }
    public Transform WorldTransform { get; }
    public Transform WorldTransformInverse { get; }
}
public interface IPhysicsComponent;

public interface IOnCreateAttribute;
public interface IOnPrepareResourcesAttribute;