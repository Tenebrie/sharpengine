using System.Reflection;
using System.Runtime.Loader;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class AssemblyReferenceNode
{
    public required string Name { get; set; }
    public required List<string> Dependencies { get; set; }
    public required HashSet<string> IsDependencyOf { get; set; }
}

public static class AssemblyRepository
{
    public static Dictionary<string, LibraryAssembly> LibraryAssemblies { get; } = new();
    public static Dictionary<string, Assembly> ExternalAssemblies { get; } = new();
    
    public static Dictionary<string, AssemblyReferenceNode> References { get; } = new();

    public static LibraryAssembly LoadLibrary(string name)
    {
        if (LibraryAssemblies.TryGetValue(name, out var result))
            return result;
        
        var newAssembly = new LibraryAssembly(name);
        LibraryAssemblies[name] = newAssembly;
        newAssembly.Load();
        return newAssembly;
    }

    public static void RegisterAssemblyDeps(string assemblyName, List<string> dependencies)
    {
        if (!References.TryGetValue(assemblyName, out var node))
        {
            node = new AssemblyReferenceNode
            {
                Name = assemblyName,
                Dependencies = dependencies,
                IsDependencyOf = References.Values.Where(depNode => depNode.Dependencies.Contains(assemblyName)).Select(depNode => depNode.Name).ToHashSet()
            };
            References[assemblyName] = node;
        }

        node.Dependencies = dependencies;
        
        References.Values.Where(depNode => node.Dependencies.Contains(depNode.Name, StringComparer.OrdinalIgnoreCase))
            .ToList()
            .ForEach(depNode => depNode.IsDependencyOf.Add(assemblyName));
    }

    public static void InvalidateDependencies(string assemblyName)
    {
        if (!References.TryGetValue(assemblyName, out var node))
            return;

        foreach (var depName in node.IsDependencyOf)
        {
            if (LibraryAssemblies.TryGetValue(depName, out var result))
                result.QueueReload();
            if (depName == Editor.RenderingAssembly.Name)
                Editor.RenderingAssembly.QueueReload();
            if (depName == Editor.GameplayAssembly.Name)
                Editor.GameplayAssembly.QueueReload();
            if (depName == Editor.PhysicsAssembly.Name)
                Editor.PhysicsAssembly.QueueReload();
            if (depName == Editor.WorkspaceAssembly.Name)
                Editor.WorkspaceAssembly.QueueReload();
        }
    }
}
