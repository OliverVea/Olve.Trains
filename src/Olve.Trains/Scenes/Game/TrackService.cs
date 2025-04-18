using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes.Game;

public class TrackService(TrackArrowRenderingService trackArrowRenderingService) : ISceneService
{
    public Result Load()
    {
        var result = trackArrowRenderingService.Load();
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track arrow rendering service");
        }

        return Result.Success();
    }

    public Result<Pass> Input()
    {
        return Pass.Pass;
    }

    public Result Update(TimeSpan deltaTime)
    {
        return Result.Success();
    }

    public Result Unload()
    {
        return Result.Success();
    }
}