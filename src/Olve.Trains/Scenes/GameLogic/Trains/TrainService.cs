using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Trains.Shared.Telemetry;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<Train> _trains = entityStoreFactory.Create<Train>();
    private int _count;

    public Event<Id<Train>> OnTrainAdded => _trains.OnAdded;
    public Event<Id<Train>> OnTrainRemoved => _trains.OnRemoved;
    public IEnumerable<Id<Train>> TrainIds => _trains.Keys;

    public int Count => _count;

    public Result<Id<Train>> AddTrain(string name)
    {
        var trainId = Id.New<Train>();
        Train train = new(trainId, name);
        if (!_trains.TryAdd(train))
        {
            return new ResultProblem("Train already exists: '{0}'", trainId);
        }

        _count++;
        if (EngineMetrics.IsEnabled) GameMetrics.TrainCount.Add(1);
        return trainId;
    }

    public DeletionResult DeleteTrain(Id<Train> trainId)
    {
        var result = _trains.Remove(trainId);
        if (!result.WasNotFound)
        {
            _count--;
            if (EngineMetrics.IsEnabled) GameMetrics.TrainCount.Add(-1);
        }
        return result;
    }
}
