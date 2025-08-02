using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.Contracts;

public interface IBaseEngineContract
{
    public Type MainBackstage { get; }
}

public interface IEngineContract<out TStage> : IBaseEngineContract where TStage : Backstage
{
    Type IBaseEngineContract.MainBackstage => typeof(TStage);
}
