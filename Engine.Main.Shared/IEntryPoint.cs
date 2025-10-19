using Engine.Core.Modules;

namespace Engine.Main.Shared;

public interface IEntryPoint
{
    public IRenderingAssembly RenderingAssembly { get; }
    public IReadOnlyList<IRootAssembly> GuestAssemblies { get; }
    public IRootHypervisor Hypervisor { get; }
}

public interface IRootAssembly
{
    public void Update(double deltaTime);
}

public interface IRenderingAssembly
{
    public void StartFrameRender();
    public void WaitUntilFrameEnd();
}