using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Common;
using Engine.Core.Extensions;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Materials;

public class MaterialInstance(Material material) : IDisposable
{
    public Material Material = material;
    public Vector4Float Tint = Vector4.One.Downgrade();
    public Vector2Float UvOffset = Vector2.Zero.Downgrade();
    public Vector2Float UvScale = Vector2.One.Downgrade();
    
    public MaterialInstance SetTintColor(Color color, double multiplier = 1.0)
    {
        Tint = (color.ToVector4() * multiplier).Downgrade();    
        return this;
    }
    public MaterialInstance SetOpacity(double opacity)
    {
        Tint.W = (float)Math.Clamp(opacity, 0.0, 1.0);
        return this;
    }
    
    public MaterialInstance SetUvOffset(Vector2 offset)
    {
        UvOffset = offset.Downgrade();
        return this;
    }
    public MaterialInstance SetUvScale(double scale)
    {
        UvScale = new Vector2(scale, scale).Downgrade();
        return this;
    }
    public MaterialInstance SetUvScale(Vector2 scale)
    {
        UvScale = scale.Downgrade();
        return this;
    }

    public MaterialInstanceSnapshot Snapshot() => new(this);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Material.RemoveInstance(this);
    }
}