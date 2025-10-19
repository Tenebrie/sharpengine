using Engine.Core.Common;
using Engine.Core.Communication.Signals;
using Engine.Core.Extensions;
using Silk.NET.Windowing;

namespace Engine.Core.Windowing;

public class WindowHandle
{
    public IWindow SystemWindow { get; }

    public Vector2 Size => SystemWindow.Size;
    public Vector2 FramebufferSize => SystemWindow.GetScaledFramebufferSize();

    public Signal Load { get; } = new();
    public Signal<Vector2> Resize { get; } = new();

    private readonly TimeSpan _resizeDebounce;
    private Vector2 _pendingResize;
    private Timer? _debounceTimer;

    public WindowHandle(IWindow baseWindow, TimeSpan? resizeDebounce = null)
    {
        _resizeDebounce = resizeDebounce ?? TimeSpan.FromMilliseconds(100); // 0.1s
        SystemWindow = baseWindow;

        SystemWindow.Load += () => Load.Emit();
        SystemWindow.Resize += size => OnSystemResize(size);
    }

    private void OnSystemResize(Vector2 size)
    {
        _pendingResize = size;

        if (_debounceTimer == null)
        {
            _debounceTimer = new Timer(_ =>
            {
                var toEmit = _pendingResize;

                _debounceTimer?.Dispose();
                _debounceTimer = null;

                EmitResize(toEmit);
            }, null, _resizeDebounce, Timeout.InfiniteTimeSpan);
        }
        else
        {
            // restart the one-shot timer to push the emit to 100ms after the latest event
            _debounceTimer.Change(_resizeDebounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void EmitResize(Vector2 size)
    {
        // If Signal must be on the UI thread, marshal here.
        Resize.Emit(size);
    }
}