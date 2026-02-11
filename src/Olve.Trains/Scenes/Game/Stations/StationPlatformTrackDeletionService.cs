using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationPlatformTrackDeletionService(ILogger<StationPlatformTrackDeletionService> logger, TrackService trackService, StationPlatformService stationPlatformService) : ISceneService
{
    private readonly EventQueue<Id<Track>> _trackAddedQueue = new(trackService.OnAdded);

    public Result Load()
    {
        _trackAddedQueue
            .SetHandler(OnRemoved)
            .Init();
        return Result.Success();
    }

    public Result Unload()
    {
        _trackAddedQueue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_trackAddedQueue
            .Update()
            .TryPickProblems(out var problems))
        {
            logger.Log(problems);
        }

        return Result.Success();
    }

    private Result OnRemoved(Id<Track> trackId) => stationPlatformService.RemoveForTrack(trackId).MapToResult();
}