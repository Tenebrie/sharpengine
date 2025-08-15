using System.Numerics;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;

namespace User.Game.Services;

public partial class ExperienceDropService : Service
{
    [Component]
    public InstancedActorComponent<ExperienceDrop> InstanceManager;
    
    [OnReady]
    protected void OnReady()
    {
        InstanceManager.Mesh = StaticMesh.CreateFromDisk("Meshes/drop-experienceOrb.obj");
        InstanceManager.Material = MaterialBuilder.Begin(typeof(ExperienceDropService)).Compile();
    }
    
    public void SpawnExperienceDrop(Vector3 position, double experienceValue)
    {
        var experienceDrop = InstanceManager.CreateInstance();
        experienceDrop.Transform.Position = position;
        experienceDrop.ExperienceValue = experienceValue;
    }
}
