using Engine.Worlds.Entities;

namespace Engine.User.Contracts;

public interface IEditorHostContract
{
    IEditorContract Editor { get; set; }
    Backstage? UserBackstage { get; set; }
    IRendererContract? Renderer { get; set; }
}