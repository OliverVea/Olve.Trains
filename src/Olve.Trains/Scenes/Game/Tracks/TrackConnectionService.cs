using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackConnectionService(TrackJunctionService trackJunctionService)
{
    public IReadOnlySet<Id<Track>> GetConnectingTracks(TrackPoint trackPoint)
    {
        var connections = trackJunctionService.GetConnections(trackPoint);

        HashSet<Id<Track>> connectingTrackIds = [];
        foreach (var (junctionTrackId, junctionTrackPoint) in connections)
        {
            var tangentDelta = trackPoint.Tangent + junctionTrackPoint.Tangent;
            if (tangentDelta.LengthSquared < 0.1f)
            {
                connectingTrackIds.Add(junctionTrackId);
            }
        }

        return connectingTrackIds;
    }
}