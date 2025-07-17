using Engine.User.Contracts;
using Engine.User.Contracts.Attributes;

namespace Engine.Editor.Host;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<HostBackstage>;
