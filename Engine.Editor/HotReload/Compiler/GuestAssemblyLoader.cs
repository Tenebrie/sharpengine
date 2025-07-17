using System.Reflection;
using System.Runtime.Loader;

namespace Engine.Editor.HotReload.Compiler;

sealed class GameAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public GameAssemblyLoadContext(string mainDll)
        : base(Path.GetFileNameWithoutExtension(mainDll), isCollectible: true)
        => _resolver = new AssemblyDependencyResolver(mainDll);

    protected override Assembly? Load(AssemblyName name)
    {
        // Share all engine assemblies
        if (name.Name?.StartsWith("Engine.", StringComparison.Ordinal) == true)
            return null;                       // let CLR fall back to default ALC

        var path = _resolver.ResolveAssemblyToPath(name);
        return path is null ? null : LoadFromAssemblyPath(path);
    }
}
