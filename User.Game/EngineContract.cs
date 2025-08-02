using Engine.Core.Contracts;
using Engine.Core.Contracts.Attributes;

namespace User.Game;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<UserBackstage>;
