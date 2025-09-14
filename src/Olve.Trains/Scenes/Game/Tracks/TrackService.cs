using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Tracks;

public class TrackService(ILoggingManager loggingManager) : BaseEntityService<Track>(loggingManager)
{
    public Result<Id<Track>> AddTrack(TrackPoint start, TrackPoint end)
    {
        var trackId = Id.New<Track>();
        Track track = new(trackId, start, end);
        return Add(track);
    }
}