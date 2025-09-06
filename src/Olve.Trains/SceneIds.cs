using Olve.Engine3D.Scenes;

namespace Olve.Trains;

public static class SceneIds
{
    public static readonly Id<IScene> GameScene = Id<IScene>.New();
    public static readonly Id<IScene> ConsoleScene = Id<IScene>.New();
    public static readonly Id<IScene> UIScene = Id<IScene>.New();
    public static readonly Id<IScene> RenderingScene = Id<IScene>.New();
}