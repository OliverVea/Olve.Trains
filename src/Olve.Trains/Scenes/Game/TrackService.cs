using Olve.Engine3D.Scenes;
using Olve.Results;

namespace Olve.Trains.Scenes.Game;

public class TrackService(TrackArrowRenderingService trackArrowRenderingService) : SceneService
{
    public override Result Load()
    {
        var result = trackArrowRenderingService.Load();
        if (result.TryPickProblems(out var problems))
        {
            return problems.Prepend("Failed to load track arrow rendering service");
        }

        return Result.Success();
    }
}