using System.Collections.Immutable;
using Olve.Engine3D.Collections;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoInventoryService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<CargoInventory> _inventories = entityStoreFactory.Create<CargoInventory>();

    private readonly Dictionary<Id<CargoInventory>, BoundedContainer<Id<CargoType>>> _containers = new();

    public Id<CargoInventory> CreateInventory(int capacity,
        ImmutableDictionary<Id<CargoType>, int>? allowedTypes = null)
    {
        var id = Id.New<CargoInventory>();
        var inventory = new CargoInventory(id, capacity, allowedTypes);
        _inventories.TryAdd(inventory);
        _containers[id] = new BoundedContainer<Id<CargoType>>(capacity, allowedTypes);
        return id;
    }

    public void RemoveInventory(Id<CargoInventory> id)
    {
        _inventories.Remove(id);
        _containers.Remove(id);
    }

    public int GetAmount(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        return _containers.TryGetValue(id, out var container) ? container.GetAmount(cargoTypeId) : 0;
    }

    public int GetTotalAmount(Id<CargoInventory> id)
    {
        return _containers.TryGetValue(id, out var container) ? container.GetTotalAmount() : 0;
    }

    public int GetRemainingCapacity(Id<CargoInventory> id)
    {
        return _containers.TryGetValue(id, out var container) ? container.GetRemainingCapacity() : 0;
    }

    public int GetRemainingCapacityForType(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        return _containers.TryGetValue(id, out var container) ? container.GetRemainingCapacityForKey(cargoTypeId) : 0;
    }

    public bool CanAccept(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        return _containers.TryGetValue(id, out var container) && container.CanAccept(cargoTypeId);
    }

    public bool TryUpdateExact(Id<CargoInventory> id, Id<CargoType> cargoTypeId, int delta)
    {
        return _containers.TryGetValue(id, out var container) && container.TryUpdateExact(cargoTypeId, delta);
    }

    public int UpdateWithinCapacity(Id<CargoInventory> id, Id<CargoType> cargoTypeId, int delta)
    {
        return _containers.TryGetValue(id, out var container) ? container.UpdateWithinCapacity(cargoTypeId, delta) : 0;
    }

    public IEnumerable<(Id<CargoType> CargoTypeId, int Amount)> GetAmounts(Id<CargoInventory> id)
    {
        if (!_containers.TryGetValue(id, out var container)) yield break;

        foreach (var (cargoTypeId, amount) in container.GetEntries())
        {
            yield return (cargoTypeId, amount);
        }
    }
}
