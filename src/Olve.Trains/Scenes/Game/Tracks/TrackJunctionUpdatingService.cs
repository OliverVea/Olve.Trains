using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackJunctionUpdatingService(ILoggingManager loggingManager, TrackService trackService, TrackJunctionService trackJunctionService) : BaseEntityListeningService<Track>(loggingManager, trackService)
{
    protected override (bool SubscribeAdd, bool SubscribeDelete) GetSubscriptions() => (true, true);

    protected override Result OnAdded(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track))
        {
            return problems;
        }
        
        trackJunctionService.AddJunctionConnection(track.Id, track.Start);
        trackJunctionService.AddJunctionConnection(track.Id, track.End);
        
        return Result.Success();
    }

    protected override Result OnRemoved(Id<Track> trackId)
    {
        if (trackService.Get(trackId).TryPickProblems(out var problems, out var track))
        {
            return problems;
        }
        
        trackJunctionService.RemoveJunctionConnection(track.Id, track.Start);
        trackJunctionService.RemoveJunctionConnection(track.Id, track.End);
        
        return Result.Success();
    }
}