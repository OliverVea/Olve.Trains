using Olve.Engine3D.Scenes;

namespace Olve.Trains;

public static class SceneIds
{
    public static readonly Id<IScene> GameScene = Id.New<IScene>();
    public static readonly Id<IScene> UIScene = Id.New<IScene>();
    public static readonly Id<IScene> RenderingScene = Id.New<IScene>();
}