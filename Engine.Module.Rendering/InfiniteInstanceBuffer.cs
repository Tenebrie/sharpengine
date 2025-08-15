using System.Drawing;
using System.Runtime.CompilerServices;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Module.Rendering;

public class InfiniteInstanceBuffer : IInstanceBuffer, IDisposable
{
    public static RenderContext Context { get; set; }

    private static readonly int SizePerInstance;
    private static readonly int PageSize;

    static InfiniteInstanceBuffer()
    {
        SizePerInstance = MatrixFloat.SizeInBytes + Vector4Float.SizeInBytes + Vector2Float.SizeInBytes + Vector2Float.SizeInBytes;
        PageSize = 8192 * SizePerInstance;
    }
    
    private readonly List<IBuffer> _buffers = [];
    private readonly List<IBufferView> _bufferViews = [];
    private int _cursorPosition = 0;
    private int _activePage = 0;
    
    private IBuffer ActiveBuffer => _buffers[_activePage];
    private IBufferView ActiveBufferView => _bufferViews[_activePage];

    private void AllocateBuffer()
    {
        var buffer = Context.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = "SharedInstanceTransformBuffer",
            Size = (ulong)PageSize,
            Usage = Usage.Dynamic,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.Write,
            Mode = BufferMode.Structured,
            ElementByteStride = (uint)SizePerInstance
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

    public InstanceBufferTicket Write(List<InstanceData> instances)
    {
        var bytesWrittenAfterSubmit = (_cursorPosition + instances.Count) * (long)SizePerInstance;
        var spaceRemaining = PageSize - bytesWrittenAfterSubmit;
        var mapFlags = MapFlags.NoOverwrite;
        if (spaceRemaining < 0)
        {
            AllocateBuffer();
            _cursorPosition = 0;
            _activePage += 1;
        }
        if (_cursorPosition == 0)
            mapFlags = MapFlags.Discard;
        
        var map = Context.DeviceContext.MapBuffer<byte>(
            ActiveBuffer,
            MapType.Write,
            mapFlags
        );

        for (var i = 0; i < instances.Count; i++)
        {
            var offset = (_cursorPosition + i) * SizePerInstance;
            var matrix = instances[i].WorldTransform.ToMatrix().Downgrade();
            Unsafe.WriteUnaligned(ref map[offset],  matrix);                offset += MatrixFloat.SizeInBytes;
            Unsafe.WriteUnaligned(ref map[offset], instances[i].Tint);      offset += Vector4Float.SizeInBytes;
            Unsafe.WriteUnaligned(ref map[offset], instances[i].UvOffset);  offset += Vector2Float.SizeInBytes;
            Unsafe.WriteUnaligned(ref map[offset], instances[i].UvScale);
        }
        Context.DeviceContext.UnmapBuffer(ActiveBuffer, MapType.Write);
        var ticket = new InstanceBufferTicket
        {
            View = ActiveBufferView,
            StartIndex = _cursorPosition,
            Count = instances.Count,
        };
            
        _cursorPosition += instances.Count;
        return ticket;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var buffer in _buffers) buffer.Dispose();
        foreach (var view in _bufferViews) view.Dispose();
        _buffers.Clear();
        _bufferViews.Clear();
    }
}