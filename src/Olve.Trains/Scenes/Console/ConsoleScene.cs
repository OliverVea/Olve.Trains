using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes.Console;

public class ConsoleScene : Scene
{
    public static readonly SceneId SceneId = new("Console");
    public override SceneId Id => SceneId;
    public override SceneLayer Layer => SceneLayer.Foreground;

    private GameScene _gameScene = null!;
    private ConsoleService _consoleService = null!;

    public override Result Load()
    {
        var gameScene = GameManager.SceneManager.Scenes.OfType<GameScene>().FirstOrDefault();
        if (gameScene is null)
        {
            return new ResultProblem("Game scene not found");
        }

        _consoleService = new ConsoleService(gameScene);
        return _consoleService.Load();
    }

    public override Result<Pass> Input()
    {
        var result = _consoleService.Input();
        if (result.TryPickProblems(out var problems, out var pass))
        {
            return problems;
        }

        return pass;
    }

    public override Result Update(TimeSpan deltaTime)
    {
        return _consoleService.Update(deltaTime);
    }

    public override void Unload()
    {
        _consoleService.Unload();
    }
}