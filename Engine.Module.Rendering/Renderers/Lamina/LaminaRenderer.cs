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
            renderable.EnsureRenderTarget(context, context);
            if (renderable.InternalTextureSize == Vector2.Zero)
                continue;
            context = new LaminaRenderContext(_textRenderer, _deviceContext);
            _renderTargetViews[0] = renderable.RenderTargetView;
            _host.FrameRenderLoop.SetRenderTargets(_renderTargetViews,
                null,
                renderable.InternalTextureSize,
                clearColor: renderable.BackgroundColor);
            renderable.CollectCommandList(context, context);
            context.FlushText();
            
            RenderStats.LaminaRerenders += 1;
            RenderStats.LaminaWidgetDrawCalls += context.RenderRequests.Count;
            RenderStats.LaminaWidgetDrawCalls += context.TextRenderRequests.Count;
            foreach (var request in context.RenderRequests)
            {
                if (request.ScissorRect is { } rect)
                {
                    _deviceContext.SetScissorRects([new Rect
                    {
                        Top = rect.Top * 2,
                        Left = rect.Left * 2,
                        Right = rect.Right * 2,
                        Bottom = rect.Bottom * 2
                    }], (uint)renderable.InternalTextureSize.X, (uint)renderable.InternalTextureSize.Y);
                }
                else
                {
                    _deviceContext.SetScissorRects([new Rect
                    {
                        Top = 0,
                        Left = 0,
                        Right = (int)renderable.InternalTextureSize.X * 2,
                        Bottom = (int)renderable.InternalTextureSize.Y * 2
                    }], (uint)renderable.InternalTextureSize.X, (uint)renderable.InternalTextureSize.Y);
                }

                request.RenderScript.Render(_deviceContext,
                    request.InstanceCount,
                    request.Mesh,
                    request.InstanceTransforms,
                    request.Material,
                    request.MaterialInstances,
                    request.ShaderParams);
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

internal class LaminaRenderContext(TextRenderer textRenderer, IDeviceContext deviceContext) : ILaminaRenderContext, ILaminaReflowContext
{
    private struct WidgetStackEntry
    {
        public required IWidget Widget { get; init; }
        public required Vector2 Position { get; set; }
        public required Vector2 SpaceTakenByChildren { get; set; }
    }

    public Vector2 ChildrenPosition
    {
        get => _widgetStack.Count == 0 ? throw new InvalidOperationException("No widget in stack") : _widgetStack.Last().Position;
        set
        {
            if (_widgetStack.Count == 0)
                throw new InvalidOperationException("No widget in stack to set position for.");
            var entry = _widgetStack.Last();
            entry.Position = value;
            _widgetStack[^1] = entry;
        }
    }

    public Vector2 OffsetToParent => _widgetStack.Count <= 1 ? throw new InvalidOperationException("No widget in stack") : _widgetStack[^2].Position;

    public Vector2 SpaceTakenByChildren
    {
        get => _widgetStack.Count <= 0 ? throw new InvalidOperationException("No widget in stack") : _widgetStack[^1].SpaceTakenByChildren;
        set
        {
            if (_widgetStack.Count <= 0)
                throw new InvalidOperationException("No widget in stack to set space taken for.");
            var entry = _widgetStack[^1];
            entry.SpaceTakenByChildren = value;
            _widgetStack[^1] = entry;
        }
    }
    public Vector2 SpaceAvailable =>
        _widgetStack.Count <= 1
            ? throw new InvalidOperationException("No widget in stack")
            : Vector2.Max(Vector2.Zero,
                Parent.ContentSize - _widgetStack[^2].SpaceTakenByChildren);
    
    public IWidget Parent => _widgetStack.Count <= 1 ? null! : _widgetStack[^2].Widget;

    private readonly List<WidgetStackEntry> _widgetStack = [];
    public void PushWidget(IWidget widget)
    {
        _widgetStack.Add(new WidgetStackEntry
        {
            Widget = widget,
            Position = Vector2.Zero,
            SpaceTakenByChildren = Vector2.Zero
        });
    }

    public void PopWidget()
    {
        if (_widgetStack.Count == 0)
            return;
        _widgetStack.RemoveAt(_widgetStack.Count - 1);
    }

    public readonly List<LaminaRenderRequest> RenderRequests = [];
    public LaminaRenderRequest GetRequest(int index) => RenderRequests[index];
    public void SetRequest(int index, LaminaRenderRequest request) => RenderRequests[index] = request;
    public int RenderRequest(LaminaRenderRequest request)
    {
        var requestIndex = RenderRequests.Count;
        RenderRequests.Add(request);
        return requestIndex;
    }


    public readonly List<LaminaTextRenderRequest> TextRenderRequests = [];
    public int RenderText(LaminaTextRenderRequest request)
    {
        var index = TextRenderRequests.Count;
        TextRenderRequests.Add(request);
        return index;
    }
    public Vector2 MeasureText(LaminaTextRenderRequest request)
    {
        return textRenderer.MeasureText(request.Font, request.Size, request.Text);
    }

    public LaminaTextRenderRequest GetTextRequest(int index) => TextRenderRequests[index];
    public void SetTextRequest(int index, LaminaTextRenderRequest request) => TextRenderRequests[index] = request;

    public void FlushText()
    {
        foreach (var request in TextRenderRequests)
        {
            textRenderer.RenderText(request.Font, request.Size, request.Text, request.Position, request.Color, request.ShadowBlur);
        }
        TextRenderRequests.Clear();
    }
}
