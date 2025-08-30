using Engine.Core.Modules;
using Engine.Core.Modules.Attributes;

namespace Engine.Module.Host;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<WorkspaceHost>;
