using Diligent;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

public interface IRenderScript
{
    public static IRenderScript Default { get; } = new RenderScript();

    public void Render(
        IDeviceContext device,
        int instanceCount,
        StaticMesh mesh,
        TransformSnapshot[] worldTransforms,
        Material material,
        MaterialInstanceSnapshot[] materialInstances);
}