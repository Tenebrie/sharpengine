using Engine.Core.Common;

namespace Engine.Core.Assets.Materials;

public struct MaterialInstanceSnapshot(MaterialInstance instance)
{
    public Vector4Float Tint = instance.Tint;
    public Vector2Float UvOffset = instance.UvOffset;
    public Vector2Float UvScale = instance.UvScale;
}