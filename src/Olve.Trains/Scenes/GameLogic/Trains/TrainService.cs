using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Shared.Telemetry;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainService(ILogger<TrainService> logger, MoneyService moneyService, EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<Train> _trains = entityStoreFactory.Create<Train>();
    private int _count;

    public Event<Id<Train>> OnTrainAdded => _trains.OnAdded;
    public Event<Id<Train>> OnTrainRemoved => _trains.OnRemoved;
    public IEnumerable<Id<Train>> TrainIds => _trains.Keys;

    public int Count => _count;

    public Result<Id<Train>> AddTrain(string name)
    {
        if (!moneyService.TryCharge(MoneyConstants.TrainCost, $"create train '{name}'"))
        {
            return new ResultProblem("Cannot afford train: need {0}, have {1}", MoneyConstants.TrainCost, moneyService.Balance);
        }

        var trainId = Id.New<Train>();
        Train train = new(trainId, name);
        if (!_trains.TryAdd(train))
        {
            return new ResultProblem("Train already exists: '{0}'", trainId);
        }

        _count++;
        if (EngineMetrics.IsEnabled) GameMetrics.TrainCount.Add(1);
        logger.LogInformation("Created train '{TrainId}' name={Name}", trainId, name);
        return trainId;
    }

    public DeletionResult DeleteTrain(Id<Train> trainId)
    {
        var result = _trains.Remove(trainId);
        if (!result.WasNotFound)
        {
            _count--;
            if (EngineMetrics.IsEnabled) GameMetrics.TrainCount.Add(-1);
            logger.LogInformation("Deleted train '{TrainId}'", trainId);
        }
        return result;
    }
}
