using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Stations;

public class StationPlatformService
{
    private readonly EntityStore<StationPlatform> _platforms;
    private readonly EntityStoreUniqueIndex<StationPlatform, Id<Track>> _platformsByTrack;

    public StationPlatformService(EntityStoreFactory entityStoreFactory)
    {
        _platforms = entityStoreFactory.Create<StationPlatform>();
        _platformsByTrack = _platforms.CreateUniqueIndex(x => x.TrackId);
    }

    public Event<Id<StationPlatform>> OnPlatformAdded => _platforms.OnAdded;
    public Event<Id<StationPlatform>> OnPlatformRemoved => _platforms.OnRemoved;

    public Result<Id<StationPlatform>> AddPlatform(Id<Track> trackId, Id<Station> stationId)
    {
        if (_platformsByTrack.ContainsKey(trackId))
        {
            return new ResultProblem("Track with id '{0}' already has a platform", trackId);
        }

        StationPlatform platform = new(Id.New<StationPlatform>(), trackId, stationId);
        _platforms.Set(platform);
        return platform.Id;
    }

    public DeletionResult RemoveForTrack(Id<Track> trackId)
    {
        if (!_platformsByTrack.TryGet(trackId, out var platformId))
        {
            return DeletionResult.NotFound();
        }

        return _platforms.Remove(platformId);
    }

    public bool TryGetPlatform(Id<Track> trackId, out Id<StationPlatform> platformId) =>
        _platformsByTrack.TryGet(trackId, out platformId);
    public bool TryGetPlatform(Id<StationPlatform> stationPlatformId, out StationPlatform platform)
        => _platforms.TryGet(stationPlatformId, out platform);

}