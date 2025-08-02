using Bgfx = Engine.Native.Bgfx.Bgfx;

namespace Engine.Core.Assets.Rendering;

public ref struct RenderContext
{
    public ushort ViewId;
    public uint InstanceTransformCount;
    public Bgfx.DynamicVertexBufferHandle InstanceTransformBuffer;
    public ushort InstanceTransformStride;

    public Span<float> InstanceTransformPrepBuffer;
}