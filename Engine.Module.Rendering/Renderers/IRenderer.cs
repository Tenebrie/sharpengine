namespace Engine.Module.Rendering.Renderers;

public interface IRenderer
{
    public void RenderFrame(double delta);
    public void RenderFrameWithTiming(double delta);
}