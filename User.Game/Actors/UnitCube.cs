using Engine.Assets.Materials;
using Engine.Assets.Meshes;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class UnitCube : Actor
{
    [Component]
    public InstancedActorComponent<UnitCubeInstance> InstanceManager;
    
    [OnInit]
    protected void OnInit()
    {
        InstanceManager.Mesh = new StaticMesh();
        InstanceManager.Material = new UnlitMaterial();
        InstanceManager.Mesh.LoadUnitCube();
    }
}

public class UnitCubeInstance : ActorInstance
{
    [OnInit]
    protected void OnInit()
    {
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        // Transform.Translate(-0.5 * deltaTime, 0, 0);
        Transform.Rotate(3 * deltaTime, 5 * deltaTime, 7 * deltaTime);
    }
}