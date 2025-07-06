using Engine.Worlds;

namespace Game.User;

public class UserSettings
{
    public Type WorldType { get; set; } = typeof(UserWorld);
}