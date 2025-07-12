using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace Game.User.Actors;

public class BouncingLogo : Actor
{
    private Vector _logoVelocity = new (143, 93, 0);
    private int _bumpCount = 0;
    private const float Acceleration = 0.1f;

    [Component] protected DebugLogoComponent DebugLogoComponent = null!;

    [OnInit]
    protected void OnInit()
    {
        // TODO: Autowire components using [Component] attribute
        DebugLogoComponent = AdoptChild(new DebugLogoComponent());
        DebugLogoComponent.Actor = this;
        var windowSize = Backstage.GetWindow().Size;
        const float debugCellSizeX = 8;
        const float debugCellSizeY = 16;
        const int logoSizeX = DebugLogoComponent.LogoSizeX;
        const int logoSizeY = DebugLogoComponent.LogoSizeY;
        Transform.Position = new Vector(windowSize.X / debugCellSizeX, windowSize.Y / debugCellSizeY, 0) / 2 - new Vector(logoSizeX, logoSizeY, 0) / 2;
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        const int logoSizeX = DebugLogoComponent.LogoSizeX;
        const int logoSizeY = DebugLogoComponent.LogoSizeY;

        Transform.Position += _logoVelocity * deltaTime;
        _logoVelocity = new Vector(Math.Sign(_logoVelocity.X), Math.Sign(_logoVelocity.Y), 0);

        if (Transform.Position.X + logoSizeX * 8 >= Backstage.GetWindow().FramebufferSize.X)
        {
            _logoVelocity.X = -1;
            Transform.Position = new Vector(Backstage.GetWindow().FramebufferSize.X - logoSizeX * 8, Transform.Position.Y, Transform.Position.Z);
            _bumpCount++;
        }
        if (Transform.Position.X < 0)
        {
            _logoVelocity.X = 1;
            Transform.Position = new Vector(0, Transform.Position.Y, Transform.Position.Z);
            _bumpCount++;
        }

        if (Transform.Position.Y + logoSizeY * 16 >= Backstage.GetWindow().FramebufferSize.Y)
        {
            _logoVelocity.Y = -1;
            Transform.Position = new Vector(Transform.Position.X, Backstage.GetWindow().FramebufferSize.Y - logoSizeY * 16, Transform.Position.Z);
            _bumpCount++;
        }
        if (Transform.Position.Y < 0)
        {
            _logoVelocity.Y = 1;
            Transform.Position = new Vector(Transform.Position.X, 0, Transform.Position.Z);
            _bumpCount++;
        }

        _logoVelocity = new Vector(_logoVelocity.X * 143, _logoVelocity.Y * 93, 0.0) * (1.0 + Acceleration * _bumpCount);
    }
}