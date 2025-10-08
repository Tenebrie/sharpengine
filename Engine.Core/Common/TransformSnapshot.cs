using System.Runtime.CompilerServices;
using Engine.Core.Makers;

namespace Engine.Core.Common;

public struct TransformSnapshot(Transform transform)
{
    public Matrix Data = transform.ToMatrix();
    
    public static TransformSnapshot Identity => new(Transform.Identity);
}
