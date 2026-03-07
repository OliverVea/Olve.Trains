namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoTransferPolicyService
{
    private readonly Dictionary<(Id<CargoInventory>, Id<CargoType>), TransferDirection> _policies = new();

    public void SetPolicy(Id<CargoInventory> inventoryId, Id<CargoType> cargoTypeId, TransferDirection direction)
    {
        if (direction == TransferDirection.None)
            _policies.Remove((inventoryId, cargoTypeId));
        else
            _policies[(inventoryId, cargoTypeId)] = direction;
    }

    public void RemoveAllPolicies(Id<CargoInventory> inventoryId)
    {
        var keysToRemove = new List<(Id<CargoInventory>, Id<CargoType>)>();
        foreach (var key in _policies.Keys)
        {
            if (key.Item1 == inventoryId) keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove) _policies.Remove(key);
    }

    public TransferDirection GetDirection(Id<CargoInventory> inventoryId, Id<CargoType> cargoTypeId)
    {
        return _policies.GetValueOrDefault((inventoryId, cargoTypeId), TransferDirection.None);
    }

    public IEnumerable<(Id<CargoType> CargoTypeId, TransferDirection Direction)> GetPolicies(Id<CargoInventory> inventoryId)
    {
        foreach (var (key, direction) in _policies)
        {
            if (key.Item1 == inventoryId) yield return (key.Item2, direction);
        }
    }
}
