using Jab;
using Olve.Engine3D;
using Olve.Engine3D.Input;
using Olve.Engine3D.Light;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Logging;
using Olve.Trains.Scenes.Console;
using Olve.Trains.Scenes.Game;
using Olve.Trains.Scenes.Game.Light;
using Olve.Trains.Scenes.Game.Time;

namespace Olve.Trains;

[ServiceProvider]
[Singleton(typeof(GameManager))]
[Singleton(typeof(SceneManager))]
[Singleton(typeof(KeyboardManager))]
[Singleton(typeof(MouseManager))]
[Singleton(typeof(DayTimeManager))]
[Singleton(typeof(DaylightManager))]
[Singleton(typeof(CommandHandlerServiceCollection))]
[Singleton(typeof(IScene), Factory = nameof(GetGameScene))]
[Singleton(typeof(IScene), Factory = nameof(GetConsoleScene))]
[Singleton(typeof(GameProvider), Factory = nameof(GetGameProvider))]
[Singleton(typeof(ILoggingManager), typeof(InMemoryLoggingManager))]
[Import(typeof(IWindowingProvider))]
[Import(typeof(IOpenGLProvider))]
public partial class GameProvider
{
    public GameProvider GetGameProvider() => this;

    public IScene GetGameScene() => new Scene<GameSceneProvider>(new GameSceneProvider(this), SceneIds.GameScene);
    public IScene GetConsoleScene() => new Scene<ConsoleSceneProvider>(new ConsoleSceneProvider(this), SceneIds.ConsoleScene);
}
