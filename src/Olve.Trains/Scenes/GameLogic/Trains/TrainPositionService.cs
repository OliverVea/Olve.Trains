using System.Collections.Concurrent;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainPositionService(TrainService trainService) : ISceneService
{
    private readonly ConcurrentDictionary<Id<Train>, TrainPositionType> _positionTypes = new();
    private readonly ConcurrentDictionary<Id<Train>, TrainTrackPosition> _trackPositions = new();
    private readonly ConcurrentDictionary<Id<Train>, TrainMotion> _motions = new();

    public Result Load()
    {
        trainService.OnTrainRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
    {
        trainService.OnTrainRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

    public IEnumerable<(Id<Train>, TrainTrackPosition)> TrackPositions => _trackPositions.Select(x => (x.Key, x.Value));

    public TrainPositionType GetPositionType(Id<Train> trainId)
    {
        return _positionTypes.GetValueOrDefault(trainId, TrainPositionType.None);
    }

    public Result SetTrackPosition(Id<Train> trainId, TrainTrackPosition trainTrackPosition)
    {
        _positionTypes[trainId] = TrainPositionType.OnTrack;
        _trackPositions[trainId] = trainTrackPosition;
        return Result.Success();
    }

    public bool TryGetTrackPosition(Id<Train> trainId, out TrainTrackPosition trainTrackPosition)
    {
        return _trackPositions.TryGetValue(trainId, out trainTrackPosition);
    }

    public Result SetMotion(Id<Train> trainId, TrainMotion motion)
    {
        _motions[trainId] = motion;
        return Result.Success();
    }

    public bool TryGetMotion(Id<Train> trainId, out TrainMotion motion)
    {
        return _motions.TryGetValue(trainId, out motion);
    }

    public IEnumerable<(Id<Train>, TrainMotion)> Motions => _motions.Select(x => (x.Key, x.Value));

    private void OnRemoved(Id<Train> id)
    {
        _positionTypes.Remove(id, out _);
        _trackPositions.Remove(id, out _);
        _motions.Remove(id, out _);
    }
}
