using Engine.User.Contracts;
using Engine.User.Contracts.Attributes;

namespace User.Game;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<UserBackstage>;
