using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Modules;
using Engine.Core.Modules.Attributes;
using Engine.Core.Profiling;
using Engine.Module.Utility.Lamina.Components;
using Engine.Module.Utility.Services;

namespace Engine.Module.Utility;

[EngineSettings]
public sealed class UserEngineContract : IEngineContract<UtilityHost>;

public partial class UtilityHost : Backstage, IUtilityHost
{
    [OnReady] 
    protected void OnReady()
    {
        RegisterService<PerformanceMonitoringService>();
        Profiler.Implementation = new Instrumentation.Profiler();
    }

    [OnCreate]
    protected void RegisterLaminaRenderers()
    {
        LaminaRendererRepository.RegisterRenderer<LaminaLayout, LaminaDiv>();
        LaminaRendererRepository.RegisterRenderer<DivLayout, LaminaDiv>();
        LaminaRendererRepository.RegisterRenderer<ButtonLayout, LaminaButton>();
        LaminaRendererRepository.RegisterRenderer<LabelLayout, LaminaLabel>();
    }
    
    [OnDestroy]
    protected void OnDestroy()
    {
        LaminaRendererRepository.Unregister<LaminaLayout>();
        LaminaRendererRepository.Unregister<DivLayout>();
        LaminaRendererRepository.Unregister<ButtonLayout>();
        LaminaRendererRepository.Unregister<LabelLayout>();
    }
}