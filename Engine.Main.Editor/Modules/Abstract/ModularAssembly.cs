using Engine.Core.Communication.Tasks;
using Engine.Core.Modules;

namespace Engine.Main.Editor.Modules.Abstract;

public abstract class ModularAssembly(string assemblyName, EngineModule module) : LibraryAssembly(assemblyName)
{
    internal EngineModule Module => module;

    internal double TimeScale = 1.0;
    
    internal abstract IModularHost? GetHost();

    public override void Unload()
    {
        if (Loader.Assembly is not null)
        {
            MainThreadTask.Purge(Loader.Assembly);
            RenderThreadTask.Purge(Loader.Assembly);
        }
        base.Unload();
    }
    
    public virtual void Destroy()
    {
        Unload();
    }
}