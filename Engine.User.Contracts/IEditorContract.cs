using Engine.Core.Enum;

namespace Engine.User.Contracts;

public interface IEditorContract
{
    public void ReloadUserGame();
    public void SetGameplayContext(GameplayContext context);
}
