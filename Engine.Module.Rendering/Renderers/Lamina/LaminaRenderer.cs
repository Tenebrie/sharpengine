using System.Drawing;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Extensions;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;
using Engine.Module.Rendering.Renderers.Atoms;
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

    private readonly ITextureView[] _renderTargetViews = new ITextureView[1];
    private void RenderRetainedTextures(ILaminaRenderable[] atomsToRender, int atomsToRenderCount)
    {
        if (atomsToRenderCount == 0)
            return;
        
        for (var index = 0; index < atomsToRenderCount; index++)
        {
            var context = new LaminaRenderContext(_textRenderer, _deviceContext);
            var renderable = atomsToRender[index];
            if (renderable is not { Dirty: true })
                continue;
            renderable.Dirty = false;
            renderable.EnsureRenderTarget();
            _renderTargetViews[0] = renderable.RenderTargetView;
            _host.FrameRenderLoop.SetRenderTargets(_renderTargetViews,
                null,
                renderable.TextureSize,
                clearColor: renderable.BackgroundColor);
            renderable.CollectCommandList(context);
            
            foreach (var request in context.RenderRequests)
            {
                if (request.ScissorRect is { } rect)
                {
                    _deviceContext.SetScissorRects([new Rect()
                    {
                        Top = rect.Top * 2,
                        Left = rect.Left * 2,
                        Right = rect.Right * 2,
                        Bottom = rect.Bottom * 2
                    }], (uint)renderable.TextureSize.X, (uint)renderable.TextureSize.Y);
                }
                request.RenderScript.Render(_deviceContext, request.InstanceCount, request.Mesh, request.InstanceTransforms, request.Material,
                    request.MaterialInstances);
            }
            _textRenderer.Flush();
        }
    }

    internal void RenderRetainedTexturesWithTiming(ILaminaRenderable[] atomsToRender, int atomsToRenderCount)
    {
        var stopwatch = Profiler.Start();
        RenderRetainedTextures(atomsToRender, atomsToRenderCount);
        stopwatch.StopAndReport(typeof(DebugProfilerFrameRenderer), ProfilingContext.RenderingLamina);
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

    public readonly List<LaminaRenderRequest> RenderRequests = [];

    public void RenderText(string font, int size, string text, Vector2 position, Color color, int shadowBlur = 2)
    {
        textRenderer.RenderText(font, size, text, position + Position, color, shadowBlur);
    }

    public void RenderRequest(LaminaRenderRequest request)
    {
        RenderRequests.Add(request);
    }
}
