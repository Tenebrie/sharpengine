namespace Engine.Module.Rendering.Renderers;

public interface IFrameRenderer
{
    public void RenderFrame(double delta);
    public void RenderFrameWithTiming(double delta);
}