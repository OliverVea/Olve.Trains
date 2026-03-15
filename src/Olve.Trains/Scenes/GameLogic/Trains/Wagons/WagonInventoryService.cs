using System.Collections.Immutable;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonInventoryService(
    TrainWagonService trainWagonService,
    WagonBlueprintService wagonBlueprintService,
    CargoInventoryService cargoInventoryService,
    EventQueueFactory eventQueueFactory) : ISceneService
{
    private readonly Dictionary<Id<Wagon>, Id<CargoInventory>> _inventories = new();

    private EventQueue<(Id<Train> TrainId, Wagon Wagon)>? _addedQueue;
    private EventQueue<(Id<Train> TrainId, Wagon Wagon)>? _removedQueue;

    public bool TryGetInventory(Id<Wagon> wagonId, out Id<CargoInventory> inventoryId)
    {
        return _inventories.TryGetValue(wagonId, out inventoryId);
    }

    public IEnumerable<Id<CargoInventory>> GetTrainInventories(Id<Train> trainId)
    {
        foreach (var wagon in trainWagonService.GetWagons(trainId))
        {
            if (_inventories.TryGetValue(wagon.Id, out var inventoryId))
            {
                yield return inventoryId;
            }
        }
    }

    public Result Load()
    {
        _addedQueue = eventQueueFactory
            .Create<(Id<Train> TrainId, Wagon Wagon)>(trainWagonService.OnWagonAdded, OnWagonAdded)
            .Init();
        _removedQueue = eventQueueFactory
            .Create<(Id<Train> TrainId, Wagon Wagon)>(trainWagonService.OnWagonRemoved, OnWagonRemoved)
            .Init();

        return Result.Success();
    }

    public Result Update()
    {
        return Result.Concat(
            _addedQueue?.Update() ?? Result.Success(),
            _removedQueue?.Update() ?? Result.Success());
    }

    public Result Unload()
    {
        _addedQueue?.Cleanup();
        _removedQueue?.Cleanup();

        foreach (var inventoryId in _inventories.Values)
        {
            cargoInventoryService.RemoveInventory(inventoryId);
        }

        _inventories.Clear();

        return Result.Success();
    }

    private Result OnWagonAdded((Id<Train> TrainId, Wagon Wagon) e)
    {
        if (!wagonBlueprintService.TryGet(e.Wagon.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Wagon blueprint not found: '{0}'", e.Wagon.BlueprintId);
        }

        ImmutableDictionary<Id<CargoType>, int>? allowedTypes = null;
        if (blueprint.AllowedTypes is not null)
        {
            var builder = ImmutableDictionary.CreateBuilder<Id<CargoType>, int>();
            foreach (var cargoTypeId in blueprint.AllowedTypes)
            {
                builder[cargoTypeId] = blueprint.Capacity;
            }

            allowedTypes = builder.ToImmutable();
        }

        var inventoryId = cargoInventoryService.CreateInventory(blueprint.Capacity, allowedTypes);
        _inventories[e.Wagon.Id] = inventoryId;

        return Result.Success();
    }

    private Result OnWagonRemoved((Id<Train> TrainId, Wagon Wagon) e)
    {
        if (_inventories.Remove(e.Wagon.Id, out var inventoryId))
        {
            cargoInventoryService.RemoveInventory(inventoryId);
        }

        return Result.Success();
    }
}
