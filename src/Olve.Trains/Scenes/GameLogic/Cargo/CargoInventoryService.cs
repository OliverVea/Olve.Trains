using System.Collections.Immutable;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoInventoryService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<CargoInventory> _inventories = entityStoreFactory.Create<CargoInventory>();

    private readonly Dictionary<Id<CargoInventory>,
        Dictionary<(Id<CargoType> CargoTypeId, Id<CargoInventory> Origin), int>> _amounts = new();

    public Id<CargoInventory> CreateInventory(int capacity,
        ImmutableDictionary<Id<CargoType>, int>? allowedTypes = null)
    {
        var id = Id.New<CargoInventory>();
        var inventory = new CargoInventory(id, capacity, allowedTypes);
        _inventories.TryAdd(inventory);
        _amounts[id] = new Dictionary<(Id<CargoType>, Id<CargoInventory>), int>();
        return id;
    }

    public void RemoveInventory(Id<CargoInventory> id)
    {
        _inventories.Remove(id);
        _amounts.Remove(id);
    }

    public int GetAmount(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        if (!_amounts.TryGetValue(id, out var amounts)) return 0;

        var total = 0;
        foreach (var (key, amount) in amounts)
        {
            if (key.CargoTypeId == cargoTypeId) total += amount;
        }

        return total;
    }

    public int GetTotalAmount(Id<CargoInventory> id)
    {
        if (!_amounts.TryGetValue(id, out var amounts)) return 0;

        var total = 0;
        foreach (var amount in amounts.Values) total += amount;
        return total;
    }

    public int GetRemainingCapacity(Id<CargoInventory> id)
    {
        if (!_inventories.TryGet(id, out var inventory)) return 0;
        return inventory.Capacity - GetTotalAmount(id);
    }

    public int GetRemainingCapacityForType(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        if (!_inventories.TryGet(id, out var inventory)) return 0;

        var remainingTotal = inventory.Capacity - GetTotalAmount(id);

        if (inventory.AllowedTypes is null) return remainingTotal;

        if (!inventory.AllowedTypes.TryGetValue(cargoTypeId, out var perTypeMax)) return 0;

        var currentOfType = GetAmount(id, cargoTypeId);
        var remainingForType = perTypeMax - currentOfType;

        return Math.Min(remainingTotal, remainingForType);
    }

    public bool CanAccept(Id<CargoInventory> id, Id<CargoType> cargoTypeId)
    {
        if (!_inventories.TryGet(id, out var inventory)) return false;
        if (inventory.AllowedTypes is null) return true;
        return inventory.AllowedTypes.ContainsKey(cargoTypeId);
    }

    public bool TryUpdateExact(Id<CargoInventory> id, Id<CargoType> cargoTypeId,
        int delta, Id<CargoInventory> origin)
    {
        if (delta == 0) return true;
        if (!_amounts.TryGetValue(id, out var amounts)) return false;
        if (!_inventories.TryGet(id, out var inventory)) return false;

        var key = (cargoTypeId, origin);

        if (delta > 0)
        {
            if (!CanAccept(id, cargoTypeId)) return false;
            if (GetRemainingCapacityForType(id, cargoTypeId) < delta) return false;

            amounts.TryGetValue(key, out var current);
            amounts[key] = current + delta;
        }
        else
        {
            amounts.TryGetValue(key, out var current);
            if (current + delta < 0) return false;

            var newAmount = current + delta;
            if (newAmount == 0)
                amounts.Remove(key);
            else
                amounts[key] = newAmount;
        }

        return true;
    }

    public int UpdateWithinCapacity(Id<CargoInventory> id, Id<CargoType> cargoTypeId,
        int delta, Id<CargoInventory> origin)
    {
        if (delta == 0) return 0;
        if (!_amounts.TryGetValue(id, out var amounts)) return 0;
        if (!_inventories.TryGet(id, out _)) return 0;

        var key = (cargoTypeId, origin);

        if (delta > 0)
        {
            if (!CanAccept(id, cargoTypeId)) return 0;
            var maxAdd = GetRemainingCapacityForType(id, cargoTypeId);
            var actualDelta = Math.Min(delta, maxAdd);
            if (actualDelta <= 0) return 0;

            amounts.TryGetValue(key, out var current);
            amounts[key] = current + actualDelta;
            return actualDelta;
        }
        else
        {
            amounts.TryGetValue(key, out var current);
            var actualDelta = Math.Max(delta, -current);
            if (actualDelta == 0) return 0;

            var newAmount = current + actualDelta;
            if (newAmount == 0)
                amounts.Remove(key);
            else
                amounts[key] = newAmount;
            return actualDelta;
        }
    }
}
