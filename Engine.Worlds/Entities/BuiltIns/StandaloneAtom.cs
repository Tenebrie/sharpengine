namespace Engine.Worlds.Entities.BuiltIns;

// This class is used to create a standalone atom that does not require a backstage.
// It can be used for testing or other purposes where a backstage is not needed.
// To submit an OnUpdate event, you can use the `ProcessLogicFrame` method directly.
public class StandaloneAtom : Atom
{
    public StandaloneAtom()
    {
        Initialize();
    }

    public new void ProcessLogicFrame(double deltaTime)
    {
        base.ProcessLogicFrame(deltaTime);
    }
}