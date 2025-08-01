﻿using Engine.Core.Common;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;

namespace User.Game.Player;

public partial class PlayerCameraFollower : Actor
{
    [Component] public MainCamera MainCameraComponent;
    
    public PlayerCharacter PlayerCharacter { get; set; }

    [OnInit]
    protected void OnInit()
    {
        MainCameraComponent.Transform.Position = new Vector3(0, 150, 0);
        MainCameraComponent.Transform.Rotation = QuatMakers.FromRotation(75, 0, 0);
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        Transform.Position += (PlayerCharacter.Transform.Position - Transform.Position) * deltaTime * 3.0f;
    }
}