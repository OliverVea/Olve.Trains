using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<Track> _tracks = entityStoreFactory.Create<Track>();

    public Event<Id<Track>> OnTrackAdded => _tracks.OnAdded;
    public Event<Id<Track>> OnTrackRemoved => _tracks.OnRemoved;

    public Result<Id<Track>> AddTrack(TrackEndpoint start, TrackEndpoint end)
    {
        var trackId = Id.New<Track>();
        Track track = new(trackId, start, end);
        if (!_tracks.TryAdd(track))
        {
            return new ResultProblem("Track already exists: '{0}'", trackId);
        }

        return trackId;
    }

    public DeletionResult DeleteTrack(Id<Track> trackId) => _tracks.Remove(trackId);

    public bool TrackExists(Id<Track> trackId) => _tracks.Exists(trackId);

    public IEnumerable<Id<Track>> TrackIds => _tracks.Keys;
    public IEnumerable<Track> Tracks => _tracks.Values;

    public bool TryGetTrack(Id<Track> trackId, out Track track) =>
        _tracks.TryGet(trackId, out track);
}
