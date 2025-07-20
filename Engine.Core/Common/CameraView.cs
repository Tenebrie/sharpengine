namespace Engine.Core.Common;

public ref struct CameraView(Span<float> viewMatrix, Span<float> projMatrix)
{
    public readonly Span<float> ViewMatrix = viewMatrix;
    public readonly Span<float> ProjMatrix = projMatrix;
}
