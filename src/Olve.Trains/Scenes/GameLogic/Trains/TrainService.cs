using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Shared.Telemetry;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainService(ILogger<TrainService> logger, MoneyService moneyService, SequenceService sequences, EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<Train> _trains = entityStoreFactory.Create<Train>();
    private EntityStoreOrderedView<Train, Id<Train>>? _trainsInCreationOrder;
    private int _count;

    public Event<EntityAdded<Train, Id<Train>>> OnTrainAdded => _trains.OnAdded;
    public Event<EntityDeleted<Train, Id<Train>>> OnTrainRemoved => _trains.OnDeleted;
    public IEnumerable<Id<Train>> TrainIds => TrainsInCreationOrder.Select(x => x.Id);
    public IEnumerable<Train> Trains => TrainsInCreationOrder;

    private EntityStoreOrderedView<Train, Id<Train>> TrainsInCreationOrder => _trainsInCreationOrder ??= _trains.CreateOrderedView(CreationOrder.Of<Train>());

    public bool TryGetTrain(Id<Train> trainId, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Train? train) => _trains.TryGet(trainId, out train);

    public int Count => _count;

    public EntityStoreColumns<Train, Id<Train>> CreateColumns() => _trains.CreateColumns();

    public Result<Id<Train>> AddTrain(string name)
    {
        if (!moneyService.TryCharge(MoneyConstants.TrainCost, $"create train '{name}'"))
        {
            return new ResultProblem("Cannot afford train: need {0}, have {1}", MoneyConstants.TrainCost, moneyService.Balance);
        }

        var trainId = Id.New<Train>();
        Train train = new(trainId, name, sequences.Next());
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
        var result = _trains.Delete(trainId);
        if (!result.WasNotFound)
        {
            _count--;
            if (EngineMetrics.IsEnabled) GameMetrics.TrainCount.Add(-1);
            logger.LogInformation("Deleted train '{TrainId}'", trainId);
        }
        return result;
    }
}
