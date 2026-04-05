using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Money;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class TrainWagonService(ILogger<TrainWagonService> logger, WagonBlueprintService wagonBlueprintService, MoneyService moneyService)
{
    private readonly Dictionary<Id<Train>, List<Wagon>> _wagons = new();

    public Event<(Id<Train> TrainId, Wagon Wagon)> OnWagonAdded { get; } = new();
    public Event<(Id<Train> TrainId, Wagon Wagon)> OnWagonRemoved { get; } = new();

    public IReadOnlyList<Wagon> GetWagons(Id<Train> trainId)
    {
        return _wagons.TryGetValue(trainId, out var list) ? list : [];
    }

    public Result<Id<Wagon>> AddWagon(Id<Train> trainId, Id<WagonBlueprint> blueprintId)
    {
        if (!wagonBlueprintService.TryGet(blueprintId, out _))
        {
            return new ResultProblem("Wagon blueprint not found: '{0}'", blueprintId);
        }

        if (!moneyService.TryCharge(MoneyConstants.WagonCost, $"add wagon to train '{trainId}'"))
        {
            return new ResultProblem("Cannot afford wagon: need {0}, have {1}", MoneyConstants.WagonCost, moneyService.Balance);
        }

        var wagon = new Wagon(Id.New<Wagon>(), blueprintId);

        if (!_wagons.TryGetValue(trainId, out var list))
        {
            list = [];
            _wagons[trainId] = list;
        }

        list.Add(wagon);
        OnWagonAdded.Invoke((trainId, wagon));

        var blueprintName = wagonBlueprintService.TryGet(blueprintId, out var bp) ? bp.Name : blueprintId.ToString();
        logger.LogInformation("Added wagon '{WagonId}' type={Type} to train '{TrainId}'", wagon.Id, blueprintName, trainId);

        return wagon.Id;
    }

    public Result RemoveWagon(Id<Train> trainId, int index)
    {
        if (!_wagons.TryGetValue(trainId, out var list) || index < 0 || index >= list.Count)
        {
            return new ResultProblem("Invalid wagon index {0} for train '{1}'", index, trainId);
        }

        var wagon = list[index];
        list.RemoveAt(index);
        OnWagonRemoved.Invoke((trainId, wagon));

        logger.LogInformation("Removed wagon '{WagonId}' at index {Index} from train '{TrainId}'", wagon.Id, index, trainId);

        return Result.Success();
    }

    public Result RemoveAllWagons(Id<Train> trainId)
    {
        if (!_wagons.TryGetValue(trainId, out var list))
        {
            return Result.Success();
        }

        foreach (var wagon in list)
        {
            OnWagonRemoved.Invoke((trainId, wagon));
        }

        list.Clear();
        _wagons.Remove(trainId);

        return Result.Success();
    }
}
