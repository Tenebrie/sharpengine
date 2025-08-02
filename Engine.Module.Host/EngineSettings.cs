using Engine.Core.Contracts;
using Engine.Core.Contracts.Attributes;

namespace Engine.Module.Host;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<HostBackstage>;
