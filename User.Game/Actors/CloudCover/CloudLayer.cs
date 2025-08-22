using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;

namespace User.Game.Actors.CloudCover;

public partial class CloudLayer : Actor
{
    [Component] protected StaticMeshComponent MeshComponent;

    private static Material _material = null!;

    [OnPrepareResources]
    protected static void OnPrepareResources()
    {
        _material = MaterialBuilder
            .CreateFromDisk("Shaders/Meshes/Clouds")
            .WithUniformPixelBuffer("CloudParams", new CloudParams(
                position: new Vector2(0.0, 0.0),
                time: 0.0,
                density: 0.5,
                sunDirection: new Vector2(1.0, 1.0).Normalized(),
                color: Color.White,
                brightness: 1.0
            ))
            .Compile();
    }

    public double LayerHeight = 0;

    [OnReady]
    protected void OnReady()
    {
        RegisterService<CloudLayerService>();
        MeshComponent.StaticMesh = PlaneMesh.Shared;
        MeshComponent.MaterialInstance = _material.Instantiate()
            .SetUvScale(25.0)
            .SetTintColor(Color.PaleGoldenrod);
    }

    private double _totalTime = Random.Shared.NextDouble() * 750.0;
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        _totalTime += deltaTime;
        var windOffset = new Vector2(_totalTime, _totalTime) * 310000 / (-LayerHeight * LayerHeight);
        // var windOffset = Vector2.Zero;
        var camera = ParentScene.Actors.OfType<Camera>().First();
        Transform.Position = new Vector3(camera.WorldTransform.Position.X, LayerHeight, camera.WorldTransform.Position.Z);
        // MeshComponent.MaterialInstance.UvOffset = ().Downgrade();
        var playerOffset = new Vector2(camera.WorldTransform.Position.X, camera.WorldTransform.Position.Z) /
                           -LayerHeight;
        MeshComponent.MaterialInstance
            .SetUvOffset(windOffset + playerOffset * 10);
    }

    public partial class CloudLayerService : Service
    {
        private double _totalTime = 0.0;

        [OnUpdate]
        protected void OnUpdate(double deltaTime)
        {
            _totalTime += deltaTime;
            _material.UpdateConstantBuffer("CloudParams", new CloudParams(
                position: new Vector2(0.0f, 0.0f),
                time: _totalTime,
                density: 0.6 + Math.Sin(_totalTime / 20.0) * 0.4,
                sunDirection: new Vector2(1.0f, 1.0f).Normalized(),
                color: Color.White,
                brightness: 1.0f
            ));
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    private readonly struct CloudParams(Vector2 position, double time, double density, Vector2 sunDirection, Color color, double brightness)
    {
        public readonly Vector2Float Position = position.Downgrade();
        public readonly float Time = (float)time;
        public readonly float Density = (float)density;
        public readonly Vector2Float SunDirection = sunDirection.Downgrade();
        public readonly Vector4Float Color = color.ToVector4().Downgrade();
        public readonly float Brightness = (float)brightness;

        public static uint SizeInBytes => (uint)Unsafe.SizeOf<CloudParams>();
    }
}
