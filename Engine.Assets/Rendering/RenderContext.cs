using Bgfx = Engine.Bindings.Bgfx.Bgfx;

namespace Engine.Assets.Rendering;

public ref struct RenderContext
{
    public ushort ViewId;
    public uint InstanceTransformCount;
    public Bgfx.DynamicVertexBufferHandle InstanceTransformBuffer;
    public ushort InstanceTransformStride;

    public Span<float> InstanceTransformPrepBuffer;
}