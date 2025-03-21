using System;
using Olve.Engine3D;
using Olve.Results;
using Olve.Trains.Scenes;
using Silk.NET.Maths;
using Silk.NET.Windowing;

var options = WindowOptions.Default with
{
    Title = "My first Silk.NET program!",
    Size = new Vector2D<int>(1920, 1080),
};

var window = Window.Create(options);

var result = RunGame();
if (result.TryPickProblems(out var problems))
{
    foreach (var problem in problems)
    {
        Console.WriteLine(problem.ToDebugString());
    }

    return -1;
}

return 0;


Result RunGame()
{
    if (GameManager.Initialize(window, [new GameScene()], GameScene.SceneId).TryPickProblems(out var p))
    {
        return p;
    }

    return GameManager.Run();
}