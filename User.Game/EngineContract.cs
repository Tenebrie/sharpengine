using Engine.Core.Contracts;
using Engine.Core.Modules.Attributes;

namespace User.Game;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<UserBackstage>;
