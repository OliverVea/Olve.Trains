using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Rendering;

public class TrackRenderingUpdaterService(
    ILoggingManager loggingManager,
    TrackService trackService,
    TrackLineStripDataService trackLineStripDataService,
    TrackRenderingService trackRenderingService) : SceneService(loggingManager)
{
    private readonly EventQueue<Id<Track>> _onTrackAddedQueue = new(trackService.OnAdded);

    public override int Priority => GetPriorityFromDependents([trackRenderingService]);

    protected override Result OnLoad()
    {
        _onTrackAddedQueue
            .SetHandler(OnTrackAdded)
            .Init();
        return Result.Success();
    }

    protected override Result OnUnload()
    {
        _onTrackAddedQueue.Cleanup();
        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime) => _onTrackAddedQueue.Update();

    private Result OnTrackAdded(Id<Track> trackId)
    {
        trackRenderingService.Unregister(trackId);

        if (trackLineStripDataService.GetLineStripData(trackId).TryPickProblems(out var problems, out var data))
        {
            return problems.Prepend("Failed to get line strip data for track");
        }

        if (trackRenderingService.Register(trackId, data).TryPickProblems(out problems, out _))
        {
            return problems.Prepend("Failed to register track");
        }

        return Result.Success();
    }
}
