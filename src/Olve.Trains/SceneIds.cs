using Olve.Engine3D.Scenes;

namespace Olve.Trains;

public static class SceneIds
{
    public static readonly Id<IScene> MainMenuScene = Id.New<IScene>();
    public static readonly Id<IScene> GameLogicScene = Id.New<IScene>();
    public static readonly Id<IScene> GameUIScene = Id.New<IScene>();
    public static readonly Id<IScene> GameRenderingScene = Id.New<IScene>();
    public static readonly Id<IScene> LoadingScene = Id.New<IScene>();
}