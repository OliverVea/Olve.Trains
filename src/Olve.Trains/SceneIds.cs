using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.Loading;

namespace Olve.Trains;

public static class SceneIds
{
    public static readonly Id<IScene> MainMenuScene = Id.New<IScene>();
    public static readonly SceneKey<GameSceneArguments> GameLogicScene = new(Id.New<IScene>());
    public static readonly Id<IScene> GameUIScene = Id.New<IScene>();
    public static readonly Id<IScene> GameRenderingScene = Id.New<IScene>();
    public static readonly SceneKey<LoadingSceneArguments> LoadingScene = new(Id.New<IScene>());
}