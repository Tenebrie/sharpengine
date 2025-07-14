using Engine.Worlds.Entities;

namespace Engine.User.Contracts;

public interface IEngineSettings<out TStage> where TStage : Backstage
{
    public Type MainBackstage => typeof(TStage);
}
