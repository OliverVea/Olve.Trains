using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes.Console;

public class ConsoleScene : Scene
{
    public static readonly SceneId SceneId = new("Console");
    public override SceneId Id => SceneId;
    public override SceneLayer Layer => SceneLayer.Foreground;

    private ConsoleService _consoleService = null!;
    private ConsoleCommandService _consoleCommandService = null!;

    public override Result Load()
    {
        _consoleCommandService = new ConsoleCommandService();
        _consoleService = new ConsoleService(_consoleCommandService);
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