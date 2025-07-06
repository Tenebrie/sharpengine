using Engine.Worlds.Entities;

namespace Engine.Worlds;

public class World
{
    public List<Unit> Units { get; } = [];
    
    protected internal virtual void OnInit() {}
    protected internal virtual void OnUpdate(double deltaTime) {}
    protected internal virtual void OnDestroy() {}
    
    public void ProcessLogicFrame(double deltaTime)
    {
        foreach (var unit in Units)
        {
            unit.OnUpdate(deltaTime);
        }
    }
}
