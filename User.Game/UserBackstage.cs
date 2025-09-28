using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using Engine.Core.Profiling.Attributes;
using User.Game.Actors;
using User.Game.Actors.BasicEnemies;
using User.Game.Actors.CloudCover;
using User.Game.Player;
using User.Game.Services;

namespace User.Game;

[MainBackstage]
public partial class UserBackstage : GameplayHostBackstage
{
    [OnReady]
    protected void OnReady()
    {
        // CreateScene<UserScene>();
        CreateScene<BasicEnemyScene>();
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
    }
}

public partial class BasicEnemyScene : Scene
{
    [OnReady]
    protected void OnReady()
    {
        RegisterService<UserInterfaceService>();
    }
}

public partial class UserScene : Scene
{
    [OnReady]
    protected void OnReady()
    {
        RegisterService<UserInputService>();
        
        var player = CreateActor<PlayerCharacter>();
        var cameraFollower = CreateActor<PlayerCameraFollower>();
        cameraFollower.PlayerCharacter = player;
        
        var honseTerrain = CreateActor<HonseTerrain>();
        honseTerrain.Transform.Position = new Vector3(-100, -5000, -2000);
        const int scale = 700;
        // honseTerrain.Transform.RotateAroundGlobal(Vector3.Up, 45);
        honseTerrain.Transform.Scale = new Vector3(scale, scale, scale);
        
        CreateActor<BasicEnemyManager>();
        var distantFog = CreateActor<DistantFogLayer>();
        distantFog.Transform.Position = new Vector3(0, -5000, 0);
        distantFog.LayerHeight = -5000;
        distantFog.RenderOffset = 100;
        distantFog.Transform.Scale = new Vector3(scale * 20, 1, scale * 20);
        distantFog.MeshComponent.MaterialInstance.SetTintColor(Color.FromArgb(60,50,50));
        distantFog.MeshComponent.MaterialInstance.SetOpacity(0.90);
        distantFog.MeshComponent.MaterialInstance.SetUvScale(75.0);
        
        // Shadow layer
        var cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.Transform.Position = new Vector3(0, -5000, 0);
        cloudLayer.LayerHeight = -5000;
        cloudLayer.RenderOffset = 100;
        cloudLayer.Transform.Scale = new Vector3(scale * 20, 1, scale * 20);
        cloudLayer.MeshComponent.MaterialInstance.SetTintColor(Color.Black);
        cloudLayer.MeshComponent.MaterialInstance.SetOpacity(2.5);
        cloudLayer.MeshComponent.MaterialInstance.SetUvScale(75.0);
        cloudLayer.IsShadow = true;
        
        const int cloudScale = 7000;
        cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.LayerHeight = -4000;
        cloudLayer.MeshComponent.MaterialInstance.SetOpacity(0.8);
        cloudLayer.Transform.Scale = new Vector3(cloudScale * 2, 1, cloudScale * 2);
        
        cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.LayerHeight = -3000;
        cloudLayer.MeshComponent.MaterialInstance.SetOpacity(0.8);
        cloudLayer.Transform.Scale = new Vector3(cloudScale * 1.5, 1, cloudScale * 1.5);
        //
        cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.Transform.Position = new Vector3(0, -1000, 0);
        cloudLayer.LayerHeight = -2000;
        cloudLayer.MeshComponent.MaterialInstance.SetOpacity(0.8);
        cloudLayer.Transform.Scale = new Vector3(cloudScale, 1, cloudScale);
        
    }
}
