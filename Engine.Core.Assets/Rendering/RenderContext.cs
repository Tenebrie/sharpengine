using Diligent;
using Engine.Core.Common;

namespace Engine.Core.Assets.Rendering;

public struct RenderContext
{
    public required IRenderDevice RenderDevice;
    public required IDeviceContext DeviceContext;
    public required ISwapChain SwapChain;
    public required IBuffer ViewMatrixBuffer;
    public required IBuffer ObjectIndexBuffer;
    public required IInstanceBuffer InstanceBuffer;
    public required IShaderSourceInputStreamFactory ShaderFactory;

    public static RenderContext Current { get; set; }
}

public interface IInstanceBuffer
{
    public InstanceBufferTicket Write(int instanceCount, InstanceData[] instances);
}

public struct InstanceData
{
    public required Transform WorldTransform;
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