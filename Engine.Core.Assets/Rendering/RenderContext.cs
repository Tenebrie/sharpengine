using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Common;

namespace Engine.Core.Assets.Rendering;

public struct RenderContext
{
    public required IRenderDevice RenderDevice;
    public required IDeviceContext DeviceContext;
    public required IDeviceContext[] DeferredContexts;
    public required ISwapChain SwapChain;
    public required IBuffer ViewMatrixBuffer;
    public required IBuffer ObjectIndexBuffer;
    public required IInstanceBuffer<InstanceData> InstanceBuffer;
    public required IShaderSourceInputStreamFactory ShaderFactory;

    public static RenderContext Current { get; set; }
}

public interface IInstanceBuffer<in T> where T : unmanaged
{
    public List<InstanceBufferTicket> Write(int instanceCount, T[] instances);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct InstanceData
{
    public required MatrixFloat WorldTransform;
    public required Vector4Float Tint;
    public required Vector2Float UvOffset;
    public required Vector2Float UvScale;
}

public struct InstanceBufferTicket
{
    public required IBufferView View;
    public required int StartIndex;
    public required int Count;
}