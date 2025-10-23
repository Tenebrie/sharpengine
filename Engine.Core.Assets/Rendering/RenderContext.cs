using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Common;

namespace Engine.Core.Assets.Rendering;

public struct RenderContext
{
    public required IRenderDevice RenderDevice;
    public required IDeviceContext ImmediateContext;
    public required IDeviceContext[] DeferredContexts;
    public required ISwapChain SwapChain;
    public required IBuffer ViewMatrixBuffer;
    public required IBuffer ObjectIndexBuffer;
    public required IInstanceBuffer<InstanceData> InstanceBuffer;
    public required IInstanceBuffer<LaminaInstanceData> LaminaInstanceBuffer;
    public required IShaderSourceInputStreamFactory ShaderFactory;
    public required Vector2 RenderTargetSize;

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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LaminaInstanceData
{
    public required MatrixFloat WorldTransform;
    public required Vector4Float Tint;
    public required Vector2Float UvOffset;
    public required Vector2Float UvScale;
    public required Vector4Float BorderRadius; // BorderRadius.x = top-left, y = top-right, z = bottom-right, w = bottom-left
}

public struct InstanceBufferTicket
{
    public required IBufferView View;
    public required int StartIndex;
    public required int Count;
}