using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Engine.Main.Editor.Modules.Compiler;

internal sealed class GameAssemblyLoadContext(string mainDll)
    : AssemblyLoadContext(Path.GetFileNameWithoutExtension(mainDll), isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(mainDll);

    protected override Assembly? Load(AssemblyName name)
    {
        // Share all engine assemblies
        if (name.Name?.StartsWith("Engine.", StringComparison.Ordinal) == true)
            return null;                       // let CLR fall back to default ALC

        var path = _resolver.ResolveAssemblyToPath(name);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
