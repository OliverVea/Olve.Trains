using Olve.Engine3D;
using Olve.Results;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Olve.Trains;

public static class Program
{
    private static readonly WindowOptions WindowOptions = WindowOptions.Default with
    {
        Title = "My first Silk.NET program!",
        Size = new Vector2D<int>(1920, 1080),
        Samples = 8
    };

    public static int Main()
    {
        var result = RunGame();
        return LogResult(result);
    }

    private static Result RunGame()
    {
        var window = Window.Create(WindowOptions);
        GameProvider gameProvider = new();

        var gameManager = gameProvider.GetService<GameManager>();
        return gameManager.Run(window, [
            SceneIds.GameScene,
            SceneIds.ConsoleScene // Doesn't work on Windows?
        ]);
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