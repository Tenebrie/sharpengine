using Engine.Assets.Meshes;
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Silk.NET.Maths;

namespace Engine.Worlds.Entities;

public partial class Camera : Actor
{
    public bool IsEditorCamera { get; protected set; } = false;
    
    private Matrix _projMatrix;
    
    [OnInit]
    internal void OnInit()
    {
        if (Backstage.Window == null)
            throw new Exception("Camera cannot be initialized without a Backstage Window.");
        
        const float fov = 60.0f * MathF.PI / 180.0f;
        var aspect = Backstage.Window.FramebufferSize.X / (float)Backstage.Window.FramebufferSize.Y;
        const float near = 0.1f;
        const float far = 10000.0f;
        var f = 1.0f / MathF.Tan(fov / 2.0f);
        
        _projMatrix = new Matrix(
            f / aspect, 0, 0, 0,
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
        var aspect = size.X / (float)size.Y;
        const float fov = 60.0f * MathF.PI / 180.0f;
        var f = 1.0f / MathF.Tan(fov / 2.0f);

        _projMatrix.M11 = f / aspect;
        _projMatrix.M22 = f;
    }

    private Transform _transformInverse = Transform.Identity;
    public CameraView AsCameraView(Span<float> viewMatrix)
    {
        WorldTransform.InverseWithoutScale(ref _transformInverse);
        _transformInverse.ToFloatSpan(ref viewMatrix);
        var projectionMatrixSpan = new float[16].AsSpan();
        _projMatrix.ToFloatSpan(ref projectionMatrixSpan);
        return new CameraView(viewMatrix, projectionMatrixSpan);
    }
    
    public struct Plane { public Vector3 Normal; public double D; }

    private Plane[] _planes = new Plane[6];
    public Plane[] UpdateFrustumPlanes()
    {
        // var vp = Matrix4x4.Multiply(view, proj);
        var vp = Matrix.Identity;
        _transformInverse.MultiplyReverse(_projMatrix, ref vp);
        var planes = new Plane[6];

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
        // No instance => use the sphere's transform directly
        if (instanceTransform == null)
        {
            foreach (var p in _planes)
            {
                if (p.Normal.DotProduct(sphere.WorldTransform.Position) + p.D < -sphere.WorldTransform.Scale.X)
                {
                    return false;
                }
            }

            return true;
        }
        
        // Instance transform provided, multiply it with the sphere's transform
        instanceTransform.Multiply(sphere.WorldTransform, ref _instanceWorldTransform);
        
        foreach (var p in _planes)
        {
            if (p.Normal.DotProduct(_instanceWorldTransform.Position) + p.D < -_instanceWorldTransform.Scale.X)
            {
                return false;
            }
        }

        return true;
    }
}