using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Logging;

namespace Engine.Module.Rendering.Utilities;

public class InfiniteInstanceWriteOnlyBuffer<T> : IInstanceBuffer<T>, IDisposable where T : unmanaged
{
    private readonly int _sizePerInstance;
    private const int EntitiesPerPage = 8192;
    private readonly int _pageSize;

    public InfiniteInstanceWriteOnlyBuffer()
    {
        _sizePerInstance = Unsafe.SizeOf<T>();
        _pageSize = EntitiesPerPage * _sizePerInstance;
    }
    
    private readonly List<IBuffer> _buffers = [];
    private readonly List<IBufferView> _bufferViews = [];
    private int _cursorPosition = 0;
    private int _activePage = 0;
    
    private IBuffer ActiveBuffer => _buffers[_activePage];
    private IBufferView ActiveBufferView => _bufferViews[_activePage];

    private void AllocateBuffer()
    {
        var buffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "SharedInstanceTransformBuffer",
            Size = (ulong)_pageSize,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.Write,
            Mode = BufferMode.Structured,
            ElementByteStride = (uint)_sizePerInstance
        });
        if (buffer == null)
            throw new InvalidOperationException("Failed to create instance buffer.");
        var bufferView = buffer.CreateView(new BufferViewDesc
        {
           Name = "SharedInstanceTransformBufferView",
           ViewType = BufferViewType.ShaderResource,
        });
        if (bufferView == null)
            throw new InvalidOperationException("Failed to create instance buffer view.");
        
        _buffers.Add(buffer);
        _bufferViews.Add(bufferView);
    }

    public void FrameStart()
    {
        if (_buffers.Count == 0)
            AllocateBuffer();
        _cursorPosition = 0;
        _activePage = 0;
    }

    public List<InstanceBufferTicket> Write(int instanceCount, T[] instances)
    {
        var pagesNeeded = (_cursorPosition + instanceCount) / EntitiesPerPage + 1;
        var instancesWritten = 0;
        var tickets = new List<InstanceBufferTicket>();
        for (var i = 0; i < pagesNeeded; i++)
        {
            var instancesInThisPage = Math.Min(instanceCount - instancesWritten, EntitiesPerPage - _cursorPosition);
            tickets.Add(new InstanceBufferTicket
            {
                View = ActiveBufferView,
                StartIndex = _cursorPosition,
                Count = instancesInThisPage,
            });
            WriteSinglePage(instancesInThisPage, instances.AsSpan(instancesWritten, instancesInThisPage));
            instancesWritten += instancesInThisPage;
            if (_cursorPosition < EntitiesPerPage)
                continue;
            
            AllocateBuffer();
            _cursorPosition = 0;
            _activePage += 1;
        }
        
        return tickets;
    }

    private void WriteSinglePage(int instanceCount, Span<T> instances)
    {
        if (instanceCount == 0)
            throw new InvalidOperationException("Instance count is 0");
        var mapFlags = _cursorPosition == 0 ? MapFlags.Discard : MapFlags.NoOverwrite;

        var map = RenderContext.Current.ImmediateContext.MapBuffer<T>(
            ActiveBuffer,
            MapType.Write,
            mapFlags);

        var dest = map.Slice(_cursorPosition, instanceCount);
        instances.CopyTo(dest);

        RenderContext.Current.ImmediateContext.UnmapBuffer(ActiveBuffer, MapType.Write);
        _cursorPosition += instanceCount;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var view in _bufferViews) view.Dispose();
        foreach (var buffer in _buffers) buffer.Dispose();
        _buffers.Clear();
        _bufferViews.Clear();
    }
}
