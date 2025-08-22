using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Contracts;

public interface IBaseEngineContract
{
    public Type MainBackstage { get; }
}

public interface IEngineContract<out TStage> : IBaseEngineContract where TStage : IHostBackstage
{
    Type IBaseEngineContract.MainBackstage => typeof(TStage);
}
