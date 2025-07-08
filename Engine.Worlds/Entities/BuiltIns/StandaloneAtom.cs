namespace Engine.Worlds.Entities.BuiltIns;

// This class is used to create a standalone atom that does not require a backstage.
// It can be used for testing or other purposes where a backstage is not needed.
// Note that this atom will not receive any lifecycle events such as initialization or rendering.
public class StandaloneAtom : Atom
{
    public StandaloneAtom()
    {
        InitializeLifecycle();
    }

    public new void ProcessLogicFrame(double deltaTime)
    {
        base.ProcessLogicFrame(deltaTime);
    }
}