using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace Game.User.Actors;

public class UnitCube : Actor
{
    [Component]
    protected StaticMeshComponent Mesh;
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        // Transform.Translate(-0.5 * deltaTime, 0, 0);
        Transform.Rotate(3 * deltaTime, 5 * deltaTime, 7 * deltaTime);
    }
}