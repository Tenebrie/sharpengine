namespace Engine.Core.Common;

public ref struct CameraView(Span<float> viewMatrix, Span<float> projMatrix)
{
    public readonly Span<float> ViewMatrix = viewMatrix;
    public readonly Span<float> ProjMatrix = projMatrix;
    
    public static CameraView Default(float viewWidth, float viewHeight, ref Span<float> viewMatrix, ref Span<float> projMatrix)
    {
        var aspect = viewWidth / viewHeight;
        const float fov = 60.0f * MathF.PI / 180.0f;
        const float near = 0.1f;
        const float far = 100.0f;
        var f = 1.0f / MathF.Tan(fov / 2.0f);

        float[] innerProjMatrix = [
            f / aspect, 0, 0, 0,
            0, f, 0, 0,
            0, 0, (far + near) / (near - far), -1,
            0, 0, (2 * far * near) / (near - far), 0
        ];
        for (var i = 0; i < 16; i++)
            projMatrix[i] = innerProjMatrix[i];

        Transform.Identity.ToFloatSpan(ref viewMatrix);
        
        return new CameraView(
            viewMatrix,
            projMatrix
        );
    }
}
