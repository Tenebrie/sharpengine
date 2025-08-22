using Engine.Main.Editor.Modules.Compiler;

namespace Engine.Main.Editor.Modules.Abstract;

public class LibraryAssembly(string assemblyName)
{
    public string Name => assemblyName;
    internal GuestAssemblyLoader Loader { get; } = new(assemblyName);
    internal List<string> Dependencies { get; } = [];

    public virtual void Load()
    {
        Loader.LoadAssembly();
        
        if (Loader.Assembly is null)
            return;
        
        foreach (var referencedAssembly in Loader.Assembly.GetReferencedAssemblies().Where(a => a.FullName.StartsWith("Engine.") || a.FullName.StartsWith("User.")))
            Dependencies.Add(referencedAssembly.Name!);
        AssemblyRepository.RegisterAssemblyDeps(assemblyName, Dependencies);
    }
    
    public virtual bool Update(double deltaTime)
    {
        return Loader.Update(deltaTime);
    }
    
    public bool NeedsReload() => Loader.AssemblyAwaitingReload;

    public void QueueReload()
    {
        Loader.AssemblyAwaitingReload = true;
    }

    public void Reload()
    {
        Loader.AssemblyAwaitingReload = false;
        Unload();
        AssemblyRepository.InvalidateDependencies(assemblyName);
        Load();
    }

    public virtual void Unload()
    {
        Loader.UnloadCurrent();
    }
}