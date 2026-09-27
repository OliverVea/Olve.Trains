using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Ordering;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

/// <summary>
/// Per-train track position and motion, stored as dense columns that follow the train store: a train gets a
/// row when it is added and loses it when it is deleted. Also records when each train arrived on its current
/// track, so trains on a track can be listed in arrival order. Main thread only.
/// </summary>
public class TrainPositionService : ISceneService
{
    private readonly SequenceService _sequences;
    private readonly EntityStoreColumns<Train, Id<Train>> _columns;
    private readonly EntityStoreColumn<TrainTrackPosition?> _trackPositions;
    private readonly EntityStoreColumn<long> _trackArrivals;
    private readonly EntityStoreColumn<TrainMotion?> _motions;

    public TrainPositionService(TrainService trainService, SequenceService sequences)
    {
        _sequences = sequences;
        _columns = trainService.CreateColumns();
        _trackPositions = _columns.AddColumn<TrainTrackPosition?>(_ => null);
        _trackArrivals = _columns.AddColumn(_ => 0L);
        _motions = _columns.AddColumn<TrainMotion?>(_ => null);
    }

    // Before every other game logic service, so a frame starts with one row per live train.
    public int Priority => -1;

    public Result Update()
    {
        _columns.Sync();
        return Result.Success();
    }

    public IEnumerable<(Id<Train>, TrainTrackPosition)> TrackPositions
    {
        get
        {
            for (var row = 0; row < _columns.Count; row++)
            {
                if (_trackPositions[row] is { } trackPosition) yield return (_columns.Ids[row], trackPosition);
            }
        }
    }

    public TrainPositionType GetPositionType(Id<Train> trainId)
    {
        return TryGetTrackPosition(trainId, out _) ? TrainPositionType.OnTrack : TrainPositionType.None;
    }

    public Result SetTrackPosition(Id<Train> trainId, TrainTrackPosition trainTrackPosition)
    {
        if (!TryGetRow(trainId, out var row))
        {
            return new ResultProblem("Cannot set track position: train '{0}' does not exist", trainId);
        }

        if (_trackPositions[row]?.TrackId != trainTrackPosition.TrackId)
        {
            _trackArrivals[row] = _sequences.Next();
        }

        _trackPositions[row] = trainTrackPosition;
        return Result.Success();
    }

    /// <summary>Returns the trains currently on <paramref name="trackId"/>, in the order they arrived on it.</summary>
    public IReadOnlyList<Id<Train>> GetTrainsOnTrack(Id<Track> trackId)
    {
        var trains = new List<(long Arrival, Id<Train> TrainId)>();
        for (var row = 0; row < _columns.Count; row++)
        {
            if (_trackPositions[row]?.TrackId == trackId) trains.Add((_trackArrivals[row], _columns.Ids[row]));
        }

        trains.Sort((a, b) => a.Arrival.CompareTo(b.Arrival));
        return trains.ConvertAll(x => x.TrainId);
    }

    public bool TryGetTrackPosition(Id<Train> trainId, out TrainTrackPosition trainTrackPosition)
    {
        if (TryGetRow(trainId, out var row) && _trackPositions[row] is { } value)
        {
            trainTrackPosition = value;
            return true;
        }

        trainTrackPosition = default;
        return false;
    }

    public Result SetMotion(Id<Train> trainId, TrainMotion motion)
    {
        if (!TryGetRow(trainId, out var row))
        {
            return new ResultProblem("Cannot set motion: train '{0}' does not exist", trainId);
        }

        _motions[row] = motion;
        return Result.Success();
    }

    public bool TryGetMotion(Id<Train> trainId, out TrainMotion motion)
    {
        if (TryGetRow(trainId, out var row) && _motions[row] is { } value)
        {
            motion = value;
            return true;
        }

        motion = default;
        return false;
    }

    public IEnumerable<(Id<Train>, TrainMotion)> Motions
    {
        get
        {
            for (var row = 0; row < _columns.Count; row++)
            {
                if (_motions[row] is { } motion) yield return (_columns.Ids[row], motion);
            }
        }
    }

    // A train added since the last sync has no row yet. Syncing here only happens for such trains, which are
    // added by commands and UI input, never while a service is iterating the rows.
    private bool TryGetRow(Id<Train> trainId, out int row)
    {
        if (_columns.TryGetRow(trainId, out row)) return true;

        _columns.Sync();
        return _columns.TryGetRow(trainId, out row);
    }
}
