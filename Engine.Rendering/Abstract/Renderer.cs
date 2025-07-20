namespace Engine.Rendering.Abstract;

public abstract class Renderer(RenderingCore parent)
{
    protected readonly RenderingCore Core = parent;

    protected internal abstract void RenderFrame(double delta);
}