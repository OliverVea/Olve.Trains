using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainTrackHistoryService
{
    public readonly record struct TrackHistoryEntry(Id<Track> TrackId, TrainDirection Direction);

    private const int MaxHistorySize = 8;

    private readonly Dictionary<Id<Train>, List<TrackHistoryEntry>> _history = new();

    public void RecordTransition(Id<Train> trainId, Id<Track> previousTrackId, TrainDirection direction)
    {
        if (!_history.TryGetValue(trainId, out var list))
        {
            list = [];
            _history[trainId] = list;
        }

        list.Insert(0, new TrackHistoryEntry(previousTrackId, direction));

        if (list.Count > MaxHistorySize)
        {
            list.RemoveAt(list.Count - 1);
        }
    }

    public IReadOnlyList<TrackHistoryEntry> GetHistory(Id<Train> trainId)
    {
        return _history.TryGetValue(trainId, out var list) ? list : [];
    }

    public Result RemoveHistory(Id<Train> trainId)
    {
        _history.Remove(trainId);
        return Result.Success();
    }
}
