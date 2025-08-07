using System.Reflection;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private static bool IsAtomType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } &&
               type != typeof(Atom) &&
               typeof(Atom).IsAssignableFrom(type);
    }

    protected Type[] GetAssemblyAtomTypes()
    {
        var ownAssembly = GetType().Assembly;
        Assembly[] assemblies = [ownAssembly];
        if (ownAssembly.FullName!.StartsWith("User.Game"))
        {
            assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.FullName!.StartsWith("Editor.Main"))
                .ToArray();
        }
        return assemblies
            .Where(a => a.FullName!.StartsWith("Engine") || a.FullName!.StartsWith("User"))
            .GroupBy(a => a.GetName().Name)
            .Select(g => g.Last())
            .Select(a =>
            {
                Console.WriteLine("Processing assembly: " + a.FullName);
                return a;
            })
            .SelectMany(a => a.GetTypes())
            .Where(IsAtomType)
            .ToArray();
    }
}
