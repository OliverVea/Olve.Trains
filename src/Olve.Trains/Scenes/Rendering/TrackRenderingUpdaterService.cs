using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Rendering;

public class TrackRenderingUpdaterService(
    EventQueueFactory eventQueueFactory,
    TrackService trackService,
    TrackLineStripDataService trackLineStripDataService,
    TrackRenderingService trackRenderingService) : ISceneService
{
    private readonly EventQueue<Id<Track>> _onTrackAddedQueue = eventQueueFactory.Create(trackService.OnAdded);

    public int Priority => SceneServicePriority.FromDependents([trackRenderingService]);

    public Result Load()
    {
        _onTrackAddedQueue
            .SetHandler(OnTrackAdded)
            .Init();
        return Result.Success();
    }

    public Result Unload()
    {
        _onTrackAddedQueue.Cleanup();
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime) => _onTrackAddedQueue.Update();

    private Result OnTrackAdded(Id<Track> trackId)
    {
        trackRenderingService.Unregister(trackId);

        if (trackLineStripDataService.GetLineStripData(trackId).TryPickProblems(out var problems, out var data))
        {
            return problems.Prepend("Failed to get line strip data for track");
        }

        if (trackRenderingService.Register(trackId, data).TryPickProblems(out problems))
        {
            return problems.Prepend("Failed to register track");
        }

        return Result.Success();
    }
}
