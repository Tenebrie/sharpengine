namespace Engine.Module.Rendering.Abstract;

public abstract class Renderer(RenderingHost parent)
{
    protected readonly RenderingHost Host = parent;

    protected internal abstract void RenderFrame(double delta);
}