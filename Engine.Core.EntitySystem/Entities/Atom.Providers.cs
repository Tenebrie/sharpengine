using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine.Core.Assets;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public static AssetManager AssetManager => AssetManager.AssemblyShared(Assembly.GetCallingAssembly());
}
