using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;
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
        RegisterService<LaminaInputService>();
        RegisterService<PerformanceMonitoringService>();
        Profiler.Implementation = new Instrumentation.Profiler();
    }

    [OnCreate]
    protected void RegisterLaminaRenderers()
    {
        LaminaRendererRepository.RegisterRenderer<LaminaLayout, LaminaDiv>();
        LaminaRendererRepository.RegisterRenderer<ButtonLayout, LaminaButton>();
        LaminaRendererRepository.RegisterRenderer<DivLayout, LaminaDiv>();
        LaminaRendererRepository.RegisterRenderer<ImageLayout, LaminaImage>();
        LaminaRendererRepository.RegisterRenderer<LabelLayout, LaminaLabel>();
        LaminaRendererRepository.RegisterRenderer<LineLayout, LaminaLine>();
    }
    
    [OnDestroy]
    protected void OnDestroy()
    {
        LaminaRendererRepository.Unregister<LaminaLayout>();
        LaminaRendererRepository.Unregister<ButtonLayout>();
        LaminaRendererRepository.Unregister<DivLayout>();
        LaminaRendererRepository.Unregister<ImageLayout>();
        LaminaRendererRepository.Unregister<LabelLayout>();
        LaminaRendererRepository.Unregister<LineLayout>();
    }
}