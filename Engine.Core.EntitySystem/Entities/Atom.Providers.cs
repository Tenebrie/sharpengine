using System.Diagnostics.CodeAnalysis;
using Engine.Core.Assets;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public AssetManager AssetManager => Backstage.SharedAssetManager;
}
