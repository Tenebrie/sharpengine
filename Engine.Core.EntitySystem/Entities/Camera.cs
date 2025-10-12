using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components;
using Engine.Core.Modules.EntitySystem;
using Silk.NET.Maths;

namespace Engine.Core.EntitySystem.Entities;

public partial class Camera : Actor, ICamera
{
    public bool IsEditorCamera { get; protected set; } = false;
    public double FieldOfView { get; set; } = 60.0;
    public double AspectRatio { get; set; } = 16.0 / 9.0;
    public double Width { get; set; } = 1920;
    public double Height { get; set; } = 1080;
    
    private Matrix _projMatrix = Matrix.Identity;
    
    [OnReady]
    internal void OnReady()
    {
        if (Backstage.Window == null)
            throw new Exception("Camera cannot be initialized without a Backstage Window.");

        Width = Backstage.Window.FramebufferSize.X;
        Height = Backstage.Window.FramebufferSize.Y;
        AspectRatio = Width / Height;
        
        const double near = 0.1;
        const double far = 20000.0;
        var f = 1.0 / Math.Tan(double.DegreesToRadians(FieldOfView) / 2.0);
        
        _projMatrix = new Matrix(
            f / AspectRatio, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (far + near) / (near - far), -1,
            0, 0, (2 * far * near) / (near - far), 0
        );
        Backstage.Window.Load += OnLoad;
        Backstage.Window.Resize += OnResize;
    }

    [OnDestroy]
    internal void OnDestroy()
    {
        Backstage.Window.Load -= OnLoad;
        Backstage.Window.Resize -= OnResize;
    }

    private void OnLoad()
    {
        OnResize(Backstage.Window.FramebufferSize);
    }

    private void OnResize(Vector2D<int> size)
    {
        Width = size.X;
        Height = size.Y;
        AspectRatio = Width / Height;
        
        var f = 1.0 / Math.Tan(double.DegreesToRadians(FieldOfView / 2.0));

        _projMatrix.M11 = f / AspectRatio;
        _projMatrix.M22 = f;
    }

    private Transform _transformInverse = Transform.Identity;
    public Transform AsCameraView()
    {
        var vp = Transform.Identity;
        
        WorldTransform.InverseWithoutScale(ref _transformInverse);
        _transformInverse.MultiplyReverse(_projMatrix, ref vp);
        return vp;
    }

    private ICamera.Plane[] _planes = new ICamera.Plane[6];
    public ICamera.Plane[] UpdateFrustumPlanes()
    {
        // var vp = Matrix4x4.Multiply(view, proj);
        var vp = Matrix.Identity;
        _transformInverse.MultiplyReverse(_projMatrix, ref vp);
        var planes = new ICamera.Plane[6];

        // left  = row4 + row1
        planes[0].Normal.X = vp.M14 + vp.M11;
        planes[0].Normal.Y = vp.M24 + vp.M21;
        planes[0].Normal.Z = vp.M34 + vp.M31;
        planes[0].D        = vp.M44 + vp.M41;

        // right = row4 - row1
        planes[1].Normal.X = vp.M14 - vp.M11;
        planes[1].Normal.Y = vp.M24 - vp.M21;
        planes[1].Normal.Z = vp.M34 - vp.M31;
        planes[1].D        = vp.M44 - vp.M41;

        // bottom = row4 + row2
        planes[2].Normal.X = vp.M14 + vp.M12;
        planes[2].Normal.Y = vp.M24 + vp.M22;
        planes[2].Normal.Z = vp.M34 + vp.M32;
        planes[2].D        = vp.M44 + vp.M42;

        // top    = row4 - row2
        planes[3].Normal.X = vp.M14 - vp.M12;
        planes[3].Normal.Y = vp.M24 - vp.M22;
        planes[3].Normal.Z = vp.M34 - vp.M32;
        planes[3].D        = vp.M44 - vp.M42;

        // near   = row4 + row3
        planes[4].Normal.X = vp.M14 + vp.M13;
        planes[4].Normal.Y = vp.M24 + vp.M23;
        planes[4].Normal.Z = vp.M34 + vp.M33;
        planes[4].D        = vp.M44 + vp.M43;

        // far    = row4 - row3
        planes[5].Normal.X = vp.M14 - vp.M13;
        planes[5].Normal.Y = vp.M24 - vp.M23;
        planes[5].Normal.Z = vp.M34 - vp.M33;
        planes[5].D        = vp.M44 - vp.M43;

        // normalize all planes
        for (var i = 0; i < 6; i++)
        {
            var n = planes[i].Normal;
            var length = n.Length;
            
            planes[i].Normal /= length;
            planes[i].D      /= length;
        }
        
        _planes = planes;
        return _planes;
    }
    
    private Transform _instanceWorldTransform = Transform.Identity;

    public bool SphereInFrustum(BoundingSphereComponent sphere, Transform? instanceTransform)
    {
        return SphereInFrustum(sphere.WorldTransform, sphere.WorldRadius, instanceTransform);
    }

    private bool SphereInFrustum(Transform worldTransform, double worldRadius, Transform? instanceTransform)
    {
        // No instance => use the sphere's transform directly
        if (instanceTransform == null)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery - introduces an allocation
            foreach (var p in _planes)
            {
                if (p.Normal.DotProduct(worldTransform.Position) + p.D < -worldRadius)
                {
                    return false;
                }
            }

            return true;
        }
        
        // Instance transform provided, multiply it with the sphere's transform
        instanceTransform.Multiply(worldTransform, ref _instanceWorldTransform);
        
        // ReSharper disable once LoopCanBeConvertedToQuery - introduces an allocation 
        foreach (var p in _planes)
        {
            if (p.Normal.DotProduct(_instanceWorldTransform.Position) + p.D < -_instanceWorldTransform.Scale.X)
            {
                return false;
            }
        }

        return true;
    }
    
    [OnUpdate]
    protected void OnReregisterOnRenderingServer()
    { 
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UpdateRegistered(Rid, this);
    }
    
    [OnDestroy]
    protected void OnUnregisterOnRenderingServer()
    {
        if (Rid == -1)
            return;
        var renderingModule = Backstage.RenderingModule;
        renderingModule?.UnregisterCamera(Rid);
    }
}