using System.Drawing;
using System.Runtime.InteropServices;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;
using Engine.Core.Logging;

namespace User.Game.Actors.CloudCover;

public partial class CloudLayer : Actor
{ 
    [Component] public StaticMeshComponent MeshComponent; 

    private static Material _material = null!;
    
    [OnPrepareResources]
    protected static void OnPrepareResources()
    {
        _material = MaterialBuilder
            .CreateFromDisk("Shaders/Meshes/Clouds")
            .WithUniformPixelBuffer("CloudParams", new CloudParams(
                time: 0.0,
                densityMin: 0.5,
                densityMax: 1.0,
                sunDirection: Vector3.One
            ))
            .AsSharedMaterial()
            .Compile();
    }

    public double LayerHeight = 0;
    public double RenderOffset = 0;
    public bool IsShadow = false;

    [OnReady]
    protected void OnReady()
    {
        RegisterService<CloudLayerService>();
        MeshComponent.StaticMesh = PlaneMesh.Shared;
        MeshComponent.MaterialInstance = _material.Instantiate()
            .SetUvScale(25.0)
            .SetTintColor(Color.White);
    }

    private double _totalTime = Random.Shared.NextDouble() * 750.0;

    private Camera _camera;
    [OnReady]
    protected void OnReadyCamera()
    {
        _camera = ParentScene.Actors.OfType<Camera>().First();
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        _totalTime += deltaTime;
        var windOffset = new Vector2(_totalTime, _totalTime) * 310000 / (-LayerHeight * LayerHeight);
        Transform.Position = new Vector3(_camera.WorldTransform.Position.X - 100, LayerHeight + RenderOffset, _camera.WorldTransform.Position.Z - 2000);
        var playerOffset = new Vector2(_camera.WorldTransform.Position.X, _camera.WorldTransform.Position.Z) / -LayerHeight;
        if (IsShadow)
        {
            MeshComponent.MaterialInstance.SetUvOffset((windOffset * 3) + playerOffset * 16);
        }
        else
        {
            MeshComponent.MaterialInstance.SetUvOffset(windOffset + playerOffset * 10);
        }
    }

    public partial class CloudLayerService : Service
    {
        private double _totalTime = 0.0;

        [OnUpdate]
        protected void OnUpdate(double deltaTime)
        {
            _totalTime += deltaTime * 10;
            RenderThreadTask.Run("CloudParams -> Update", () =>
            {
                _material.UpdateConstantBuffer("CloudParams", new CloudParams(
                    time: _totalTime,
                    densityMin: 0.4,
                    densityMax: 0.6,
                    sunDirection: new Vector3(0, 0.0, 1.0).Normalized()
                ));
            });
        }
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    private readonly struct CloudParams(double time, double densityMin, double densityMax, Vector3 sunDirection)
    {
        public readonly float Time = (float)time;
        public readonly float DensityMaskMin = (float)densityMin;
        public readonly float DensityMaskMax = (float)densityMax;
        public readonly Vector3Float SunDirection = sunDirection.Downgrade();
        private readonly Vector2Float _padding = Vector2.Zero.Downgrade();
    }
}
