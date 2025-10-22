using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using User.Game.Player.Components;

namespace User.Game.FirstPerson;

public partial class FirstPersonWeapon : ActorComponent
{
    [Component] public DragonMesh Weapon;

    [OnReady]
    protected void OnReady()
    {
        Transform.Position = (0.2, -0.2, -0.15);
        Transform.RotateAroundGlobal(Vector3.Forward, 15);
        Transform.Scale = (0.015, 0.015, 0.015);
    }
}