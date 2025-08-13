using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

public interface IRenderScript
{
    public static IRenderScript Default { get; } = new RenderScript();

    public void Render(
        int instanceCount,
        StaticMesh mesh,
        Transform[] worldTransforms,
        Material material,
        MaterialInstance[] materialInstances);
}