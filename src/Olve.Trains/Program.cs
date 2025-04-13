using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Results;
using Olve.Trains.Scenes;
using Olve.Trains.Scenes.Console;
using Olve.Trains.Scenes.Game;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Olve.Trains;

public static class Program
{
    private static readonly WindowOptions WindowOptions = WindowOptions.Default with
    {
        Title = "My first Silk.NET program!",
        Size = new Vector2D<int>(1280, 720),
        Samples = 8
    };

    private static readonly Scene[] Scenes =
    [
        new ConsoleScene(),
        new GameScene()
    ];

    private static readonly SceneId[] SceneIds =
    [
        GameScene.SceneId,
        ConsoleScene.SceneId
    ];

    public static int Main()
    {
        var result = RunGame();
        return LogResult(result);
    }

    private static Result RunGame()
    {
        var window = Window.Create(WindowOptions);

        var initializationResult = GameManager.Initialize(window, Scenes, SceneIds);
        if (initializationResult.TryPickProblems(out var problems))
        {
            return problems;
        }

        GameManager.LoggingManager = new ConsoleLoggingManager();

        return GameManager.Run();
    }

    private static int LogResult(Result result)
    {
        if (result.TryPickProblems(out var problems))
        {
            foreach (var problem in problems)
            {
                Console.WriteLine(problem.ToDebugString());
            }

            return 1;
        }

        Console.WriteLine("Game exited successfully.");
        return 0;
    }

}