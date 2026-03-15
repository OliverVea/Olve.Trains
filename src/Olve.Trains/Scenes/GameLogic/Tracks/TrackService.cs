using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Trains.Shared.Telemetry;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackService(
    EntityStoreFactory entityStoreFactory,
    TrainPositionService trainPositionService,
    TrainService trainService,
    TrainTrackHistoryService trainTrackHistoryService)
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

        if (EngineMetrics.IsEnabled) GameMetrics.TrackCount.Add(1);
        return trackId;
    }

    public bool CanDeleteTrack(Id<Track> trackId) => !IsTrackOccupied(trackId);

    public DeletionResult DeleteTrack(Id<Track> trackId)
    {
        if (!CanDeleteTrack(trackId))
        {
            return DeletionResult.Error(new ResultProblem("Cannot delete track '{0}': a train or wagon is on it", trackId));
        }

        var result = _tracks.Remove(trackId);
        if (EngineMetrics.IsEnabled && !result.WasNotFound) GameMetrics.TrackCount.Add(-1);
        return result;
    }

    private bool IsTrackOccupied(Id<Track> trackId)
    {
        foreach (var (_, trackPosition) in trainPositionService.TrackPositions)
        {
            if (trackPosition.TrackId == trackId)
            {
                return true;
            }
        }

        foreach (var trainId in trainService.TrainIds)
        {
            foreach (var entry in trainTrackHistoryService.GetHistory(trainId))
            {
                if (entry.TrackId == trackId)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool TrackExists(Id<Track> trackId) => _tracks.Exists(trackId);

    public IEnumerable<Id<Track>> TrackIds => _tracks.Keys;
    public IEnumerable<Track> Tracks => _tracks.Values;

    public bool TryGetTrack(Id<Track> trackId, out Track track) =>
        _tracks.TryGet(trackId, out track);
}
