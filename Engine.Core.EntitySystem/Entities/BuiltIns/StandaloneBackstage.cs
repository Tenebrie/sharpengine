namespace Engine.Core.EntitySystem.Entities.BuiltIns;

// This class is used to create a standalone backstage that exposes events usually called by the window.
// It can be used primarily for testing.
public partial class StandaloneBackstage : Backstage
{
    public StandaloneBackstage(bool skipInit = false)
    {
        if (!skipInit)
        {
            Initialize();
        }
    }
    public new void Initialize() => base.Initialize();
    public new void ProcessLogicFrame(double deltaTime) => base.ProcessLogicFrame(deltaTime);
}
