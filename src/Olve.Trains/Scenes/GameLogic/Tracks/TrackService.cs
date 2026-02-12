using Microsoft.Extensions.Logging.Abstractions;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackService() : BaseEntityService<Track>(NullLogger.Instance)
{
    public Result<Id<Track>> AddTrack(TrackEndpoint start, TrackEndpoint end)
    {
        var trackId = Id.New<Track>();
        Track track = new(trackId, start, end);
        return Add(track);
    }
}