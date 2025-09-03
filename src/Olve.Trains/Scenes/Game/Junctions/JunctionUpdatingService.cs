using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Junctions;

public class JunctionUpdatingService(
    ILoggingManager loggingManager,
    TrackService trackService,
    JunctionService junctionService) : BaseEntityListeningService<Track>(loggingManager, trackService)
{
    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (true, true);

    protected override Result OnAdded(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track)
            || junctionService.AddJunctionConnection(track.Id, track.Start).TryPickProblems(out problems)
            || junctionService.AddJunctionConnection(track.Id, track.End).TryPickProblems(out problems))
        {
            return problems;
        }

        return Result.Success();
    }

    protected override Result OnRemoved(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track)
            || junctionService.RemoveJunctionConnection(track.Id, track.Start).TryPickProblems(out problems)
            || junctionService.RemoveJunctionConnection(track.Id, track.End).TryPickProblems(out problems))
        {
            return problems;
        }

        return Result.Success();
    }
}