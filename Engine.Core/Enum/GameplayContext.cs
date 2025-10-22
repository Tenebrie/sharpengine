namespace Engine.Core.Enum;

[Flags]
public enum GameplayContext
{
    StandalonePlay,
    EmbeddedPlay,
    Editor,
}