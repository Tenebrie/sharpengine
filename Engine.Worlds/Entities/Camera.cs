using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Silk.NET.Maths;

namespace Engine.Worlds.Entities;

public class Camera : Actor
{
    private float[] _projMatrix;
    
    [OnInit]
    internal void OnInit()
    {
        if (Backstage.Window == null)
            throw new Exception("Camera cannot be initialized without a Backstage Window.");
        
        const float fov = 60.0f * MathF.PI / 180.0f;
        var aspect = Backstage.Window.FramebufferSize.X / (float)Backstage.Window.FramebufferSize.Y;
        const float near = 0.1f;
        const float far = 100.0f;
        var f = 1.0f / MathF.Tan(fov / 2.0f);

        _projMatrix = [
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (far + near) / (near - far), -1,
            0, 0, (2 * far * near) / (near - far), 0
        ];
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

        _projMatrix[0] = f / aspect;
        _projMatrix[5] = f;
    }

    public CameraView AsCameraView(Span<float> viewMatrix)
    {
        Transform.Negate().ToFloatSpan(ref viewMatrix);
        return new CameraView(viewMatrix, _projMatrix.AsSpan());
    }
}