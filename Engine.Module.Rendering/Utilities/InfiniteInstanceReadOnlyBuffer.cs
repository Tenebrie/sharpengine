using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets.Rendering;
using Engine.Core.Logging;

namespace Engine.Module.Rendering.Utilities;

public class InfiniteInstanceReadOnlyBuffer<T> : IDisposable where T : unmanaged
{
    private readonly int _sizePerInstance;
    private const int EntitiesPerPage = 8192;
    private readonly int _pageSize;
    
    public InfiniteInstanceReadOnlyBuffer()
    {
        _sizePerInstance = Unsafe.SizeOf<T>();
        _pageSize = EntitiesPerPage * _sizePerInstance;
    }
    
    private readonly List<IBuffer> _gpuBuffers = [];
    private readonly List<IBufferView> _gpuBufferViews = [];
    
    private readonly List<IBuffer> _stagingBuffers = [];
    
    private void AllocateBuffer()
    {
        Logger.Info("Allocati");
        var writeBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = $"InstanceBuffer<{typeof(T).Name}>_{_gpuBuffers.Count}",
            Size = (ulong)_pageSize,
            Usage = Usage.Default,
            BindFlags = BindFlags.UnorderedAccess,
            CPUAccessFlags = CpuAccessFlags.None,
            Mode = BufferMode.Structured,
            ElementByteStride = (uint)_sizePerInstance
        });
        if (writeBuffer == null)
            throw new InvalidOperationException("Failed to create write buffer.");
        
        var writeBufferView = writeBuffer.CreateView(new BufferViewDesc
        {
           ViewType = BufferViewType.UnorderedAccess,
        });
        if (writeBufferView == null)
            throw new InvalidOperationException("Failed to create write buffer view.");
        
        var stagingBuffer = RenderContext.Current.RenderDevice.CreateBuffer(new BufferDesc
        {
            Name = $"InstanceStagingBuffer<{typeof(T).Name}>_{_stagingBuffers.Count}",
            Size = (ulong)_pageSize,
            Usage = Usage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            Mode = BufferMode.Structured,
            ElementByteStride = (uint)_sizePerInstance
        });
        if (stagingBuffer == null)
            throw new InvalidOperationException("Failed to create instance buffer view.");
        
        _gpuBuffers.Add(writeBuffer);
        _stagingBuffers.Add(stagingBuffer);
        _gpuBufferViews.Add(writeBufferView);
    }

    public struct InstanceBufferTicket
    {
        public required IBufferView View;
    }
    public List<InstanceBufferTicket> GetBindTickets(int instanceCount)
    {
        var (pagesNeeded, _) = CountPages(instanceCount);
        var pagesToCreate = pagesNeeded - _stagingBuffers.Count;
        while (pagesToCreate > 0)
        {
            AllocateBuffer();
            pagesToCreate -= 1;
        }

        return _gpuBufferViews.Take(pagesNeeded).Select(view => new InstanceBufferTicket{ View = view }).ToList();
    }

    private (int pagesNeeded, long bytesToRead) CountPages(int instanceCount)
    {
        var bytesToRead = instanceCount * (long)_sizePerInstance;
        var pagesNeeded = instanceCount / EntitiesPerPage + 1;
        if (pagesNeeded > 64)
            throw new InvalidOperationException("Seems to require way too many pages.");
        return (pagesNeeded, bytesToRead);
    }
    
    public void DownloadLatestState(int instanceCount)
    {
        var (pagesNeeded, bytesToRead) = CountPages(instanceCount);
        
        var bytesRemaining = bytesToRead;
        for (var i = 0; i < pagesNeeded; i++)
        {
            var src = _gpuBuffers[i];
            var dst = _stagingBuffers[i];

            var bytesThisPage = Math.Min(_pageSize, bytesRemaining);
            RenderContext.Current.ImmediateContext.CopyBuffer(src,
                0,
                ResourceStateTransitionMode.Transition,
                dst,
                0,
                (ulong)bytesThisPage,
                ResourceStateTransitionMode.Transition);

            bytesRemaining -= bytesThisPage;
        }
    }

    public void Read(int instanceCount, ref List<T> data)
    {
        data.Clear();
        var (pagesNeeded, _) = CountPages(instanceCount);
        
        var remainingElems = instanceCount;
        for (var i = 0; i < pagesNeeded; i++)
        {
            var elemsThisPage = Math.Min(remainingElems, EntitiesPerPage);
            if (elemsThisPage <= 0)
                break;

            var spanBytes = RenderContext.Current.ImmediateContext.MapBuffer<byte>(_stagingBuffers[i], MapType.Read, MapFlags.DoNotWait);
            var spanT = MemoryMarshal.Cast<byte, T>(spanBytes);
            data.AddRange(spanT[..elemsThisPage]);
            RenderContext.Current.ImmediateContext.UnmapBuffer(_stagingBuffers[i], MapType.Read);

            remainingElems -= elemsThisPage;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var view in _gpuBufferViews) view.Dispose();
        foreach (var buffer in _gpuBuffers) buffer.Dispose();
        foreach (var buffer in _stagingBuffers) buffer.Dispose();
        _gpuBuffers.Clear();
        _gpuBufferViews.Clear();
        _stagingBuffers.Clear();
    }
}