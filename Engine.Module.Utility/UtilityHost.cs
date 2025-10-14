using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Modules;
using Engine.Core.Modules.Attributes;
using Engine.Core.Profiling;
using Engine.Module.Utility.Services;

namespace Engine.Module.Utility;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<UtilityHost>;

public partial class UtilityHost : Backstage, IUtilityHost
{
    [OnReady] 
    protected void OnReady()
    {
        RegisterService<LaminaInputService>();
        RegisterService<PerformanceMonitoringService>();
        Profiler.Implementation = new Instrumentation.Profiler();
    }
}