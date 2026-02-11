using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionUpdatingService(
    ILogger<JunctionUpdatingService> logger,
    TrackService trackService,
    JunctionService junctionService) : ISceneService
{
    public Result Load()
    {
        trackService.OnAdded.Subscribe(OnAdded);
        trackService.OnRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
    {
        trackService.OnAdded.Unsubscribe(OnAdded);
        trackService.OnRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

    private void OnAdded(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track)
            || junctionService.AddJunctionConnection(track.Id, track.Start).TryPickProblems(out problems)
            || junctionService.AddJunctionConnection(track.Id, track.End).TryPickProblems(out problems))
        {
            logger.Log(problems.Prepend("Got failure while updating junction"));
        }
    }

    private void OnRemoved(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track)
            || junctionService.RemoveJunctionConnection(track.Id, track.Start).TryPickProblems(out problems)
            || junctionService.RemoveJunctionConnection(track.Id, track.End).TryPickProblems(out problems))
        {
            logger.Log(problems.Prepend("Got failure while updating junction when track with id '{0}' was removed", trackId));
        }
    }
}