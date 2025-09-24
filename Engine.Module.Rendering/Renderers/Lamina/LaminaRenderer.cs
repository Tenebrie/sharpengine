using System.Drawing;
using Diligent;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Lamina;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Renderers.Debug;
using Engine.Module.Rendering.Renderers.Fonts;

namespace Engine.Module.Rendering.Renderers.Lamina;

internal class LaminaRenderer : IDisposable
{
    private readonly RenderingHost _host;
    private readonly IDeviceContext _deviceContext;
    private readonly TextRenderer _textRenderer;
    internal LaminaRenderer(RenderingHost host, IDeviceContext deviceContext)
    {
        _host = host;
        _deviceContext = deviceContext;
        _textRenderer = new TextRenderer(deviceContext);
    }

    private ICommandList? RenderRetainedTextures(ILaminaRenderable[] atomsToRender, int atomsToRenderCount)
    {
        if (atomsToRenderCount == 0)
            return null;
        
        _deviceContext.Begin(0);
        var context = new LaminaRenderContext(_textRenderer, _deviceContext);
        for (var index = 0; index < atomsToRenderCount; index++)
        {
            var renderable = atomsToRender[index];
            if (renderable is not { Dirty: true })
                continue;
            renderable.Dirty = false;
            renderable.EnsureRenderTarget(); 
            _deviceContext.ClearRenderTarget(
                renderable.RenderTargetView,
                new Vector4(0.25f, 0.25f, 0.25f, 1.0f),
                ResourceStateTransitionMode.Transition);
            _deviceContext.SetRenderTargets([renderable.RenderTargetView], null, ResourceStateTransitionMode.Transition);
            renderable.CollectCommandList(context);
            _textRenderer.Flush();
        }
        
        return _deviceContext.FinishCommandList();
    }

    internal ICommandList? RenderRetainedTexturesWithTiming(ILaminaRenderable[] atomsToRender, int atomsToRenderCount)
    {
        var stopwatch = Profiler.Start();
        var cmdList = RenderRetainedTextures(atomsToRender, atomsToRenderCount);
        stopwatch.StopAndReport(typeof(DebugProfilerFrameRenderer), ProfilingContext.RenderingLamina);
        return cmdList;
    }

    public void Dispose()
    {
        _textRenderer.Dispose();
    }
}

internal class LaminaRenderContext(TextRenderer textRenderer, IDeviceContext deviceContext) : ILaminaRenderContext
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public IDeviceContext DeviceContext => deviceContext;

    public void RenderText(string font, int size, string text, Vector2 position, Color color, int shadowBlur = 0)
    {
        textRenderer.RenderText(font, size, text, position + Position, color, shadowBlur);
    }

    public void RenderRequest(LaminaRenderRequest request)
    {
        // throw new NotImplementedException();
    }
}
