using System.Diagnostics.CodeAnalysis;
using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging.Abstractions;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Stations;

public class StationPlatformService() : BaseEntityService<StationPlatform>(NullLogger.Instance)
{
    private readonly Dictionary<Id<Track>, Id<StationPlatform>> _platformsByTrack = new();

    public Result<Id<StationPlatform>> AddPlatform(Id<Track> trackId, Id<Station> stationId)
    {
        if (_platformsByTrack.ContainsKey(trackId))
        {
            return new ResultProblem("Track with id '{0}' already has a platform", trackId);
        }

        var platformId = Id.New<StationPlatform>();
        _platformsByTrack.Add(trackId, platformId);
        StationPlatform platform = new(platformId, trackId, stationId);

        return Add(platform);
    }
    
    public DeletionResult RemoveForTrack(Id<Track> trackId)
    {
        if (!_platformsByTrack.Remove(trackId, out var platformId))
        {
            return DeletionResult.NotFound();
        }

        return Remove(platformId);
    }

    public bool TryGetPlatform(Id<Track> trackId, [MaybeNullWhen(false)] out Id<StationPlatform> platformId)
    {
        return _platformsByTrack.TryGetValue(trackId, out platformId);
    }
}