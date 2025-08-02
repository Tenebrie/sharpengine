namespace Engine.Module.Rendering.Abstract;

public abstract class Renderer(RenderingModule parent)
{
    protected readonly RenderingModule Module = parent;

    protected internal abstract void RenderFrame(double delta);
}