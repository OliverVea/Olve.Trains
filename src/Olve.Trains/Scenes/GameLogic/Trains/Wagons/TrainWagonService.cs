using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class TrainWagonService(WagonBlueprintService wagonBlueprintService)
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

        var wagon = new Wagon(Id.New<Wagon>(), blueprintId);

        if (!_wagons.TryGetValue(trainId, out var list))
        {
            list = [];
            _wagons[trainId] = list;
        }

        list.Add(wagon);
        OnWagonAdded.Invoke((trainId, wagon));

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
