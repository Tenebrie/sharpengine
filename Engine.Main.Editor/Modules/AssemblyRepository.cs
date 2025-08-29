using System.Reflection;
using Engine.Core.Logging;
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
    public static HashSet<string> AssembliesAwaitingReload { get; } = [];

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

    public static bool InvalidateDependencies(string assemblyName)
    {
        if (!References.TryGetValue(assemblyName, out var node))
            return false;

        var anyInvalidated = false;
        Logger.Info("Invalidating deps for " + assemblyName);
        AssembliesAwaitingReload.Add(assemblyName);
        foreach (var depName in node.IsDependencyOf)
        {
            if (!AssembliesAwaitingReload.Add(depName))
                continue;
            
            anyInvalidated = true;
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
        return anyInvalidated;
    }

    private static void UpdateReloadPriority(List<LibraryAssembly> assemblies)
    {
        var currentIteration = 0;
        while (currentIteration++ < 32)
        {
            var changed = false;
            foreach (var higherAssembly in assemblies)
            {
                foreach (var deeperAssemblyName in higherAssembly.Dependencies)
                {
                    var deeperAssembly = assemblies.Find(a => a.Name == deeperAssemblyName);
                    if (deeperAssembly == null || deeperAssembly.ReloadPriority > higherAssembly.ReloadPriority)
                        continue;
                    
                    changed = true;
                    Logger.Warn("Bumping " + deeperAssemblyName);
                    deeperAssembly.ReloadPriority = higherAssembly.ReloadPriority + 1;
                }
            }

            if (!changed)
                return;
        }
        throw new Exception("Failed to resolve assembly reload priorities, circular dependency detected.");
    }

    public static List<ModularAssembly> ReloadAllAwaiting()
    {
        if (AssembliesAwaitingReload.Count == 0)
            return [];
        
        var allAssemblies = LibraryAssemblies.Values.ToList();
        allAssemblies.Add(Editor.RenderingAssembly);
        allAssemblies.Add(Editor.GameplayAssembly);
        allAssemblies.Add(Editor.PhysicsAssembly);
        allAssemblies.Add(Editor.WorkspaceAssembly);

        UpdateReloadPriority(allAssemblies);
        
        var sortedAssemblies = allAssemblies
            .Where(assembly => assembly.NeedsReload())
            .OrderByDescending(assembly => assembly.ReloadPriority)
            .ToList();
        var reversedSortedAssemblies = sortedAssemblies.AsEnumerable().Reverse().ToList();
        foreach (var assembly in reversedSortedAssemblies)
        {
            assembly.Unload();
        }
        Logger.Info("Load order: " + string.Join(", ", sortedAssemblies.Select(a => a.Name + " (priority " + a.ReloadPriority + ")")));
        Logger.Info("Unload order: " + string.Join(", ", reversedSortedAssemblies.Select(a => a.Name + " (priority " + a.ReloadPriority + ")")));
        foreach (var assembly in sortedAssemblies)
        {
            assembly.Load();
        }
        AssembliesAwaitingReload.Clear();
        
        return sortedAssemblies.Where(assembly => assembly is ModularAssembly).Cast<ModularAssembly>().ToList();
    }
}
