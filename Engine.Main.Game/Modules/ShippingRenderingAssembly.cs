using System.Diagnostics;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;
using Engine.Core.Memory;
using Engine.Core.Modules;
using Engine.Core.Profiling;
using Engine.Main.Game.Modules.Abstract;
using Engine.Main.Shared;
using Engine.Module.Rendering;

namespace Engine.Main.Game.Modules;

internal sealed class ShippingRenderingAssembly(IEntryPoint entryPoint) : BundledAssembly("Engine.Module.Rendering", EngineModule.Rendering), IRenderingAssembly
{
    internal RenderingHost RenderingHost { get; private set; } = null!;
    private RenderingHostBootstrap RenderingBootstrap { get; set; } = null!;
    private RenderingResources Resources { get; set; }
    
    internal override IModularHost GetHost() => RenderingHost;

    internal override void Load()
    {
        base.Load();
        RenderingHost = new RenderingHost
        {
            Hypervisor = entryPoint.Hypervisor
        };
        RenderingBootstrap = new RenderingHostBootstrap
        {
            Hypervisor = entryPoint.Hypervisor
        };
        
        Resources = RenderingBootstrap.Initialize();
        RenderingHost.InitializeResources(Resources);
        RenderingHost.RenderEngineLoadingScreen();
        RenderingHost.InitializeRenderers();
        StartRenderThread();
    }
    
    private Thread? _renderThread;
    private enum RenderThreadState { Stopped, Running }
    private volatile RenderThreadState _renderThreadState = RenderThreadState.Stopped;
    private void StartRenderThread()
    {
        if (_renderThread is { IsAlive: true } && _renderThreadState == RenderThreadState.Running)
            return;
        
        _renderThreadState = RenderThreadState.Running;
        _renderThread = new Thread(RenderThreadLoop)
        {
            Name = "RenderThread",
            IsBackground = false
        };
        _renderThread.Start();
    }
    
    private readonly Barrier _renderStartBarrier = new(2);
    private readonly Barrier _renderEndBarrier = new(2);
    private void RenderThreadLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            double lastFrameTime = 0;

            while (_renderThreadState != RenderThreadState.Stopped)
            {
                FrameCounter.Increment();
                RenderStats.Reset();
                
                _renderStartBarrier.SignalAndWait();
                if (_renderThreadState == RenderThreadState.Stopped)
                    break;
                
                var currentTime = stopwatch.Elapsed.TotalMicroseconds;
                var deltaTime = (currentTime - lastFrameTime) / 1000000.0;
                lastFrameTime = currentTime;
                
                RenderingHost.RenderSingleFrame(deltaTime);
                RenderThreadTask.ExecuteAllQueued();
                MemoryManager.FreeDomain(MemoryDomain.Rendering);
                
                _renderEndBarrier.SignalAndWait();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"RenderingAssembly: Render thread crashed: {ex}");
            throw;
        }
    }

    public void StartFrameRender() => _renderStartBarrier.SignalAndWait();
    public void WaitUntilFrameEnd() => _renderEndBarrier.SignalAndWait();

    private void StopRenderThread()
    {
        _renderThreadState = RenderThreadState.Stopped;
        _renderEndBarrier.RemoveParticipant();
        _renderStartBarrier.RemoveParticipant();
        _renderThread?.Join();
        _renderThread = null;
    }

    public override void Update(double deltaTime) {}

    internal void Destroy()
    {
        StopRenderThread();
        RenderingHost.HotShutdown();
        RenderingBootstrap.Shutdown();
    }
}