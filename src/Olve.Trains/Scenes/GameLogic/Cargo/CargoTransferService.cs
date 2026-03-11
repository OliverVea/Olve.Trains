using Microsoft.Extensions.Logging;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoTransferService(ILogger<CargoTransferService> logger, CargoInventoryService cargoInventoryService)
{
    private readonly record struct InventoryChange(
        Id<CargoInventory> InventoryId,
        Id<CargoType> CargoTypeId,
        int Delta);

    public int Transfer(Id<CargoInventory> from, Id<CargoInventory> to,
        Id<CargoType> cargoTypeId, int maxAmount)
    {
        if (maxAmount <= 0) return 0;

        var available = cargoInventoryService.GetAmount(from, cargoTypeId);
        var capacity = cargoInventoryService.GetRemainingCapacityForType(to, cargoTypeId);
        var amount = Math.Min(maxAmount, Math.Min(available, capacity));

        if (amount <= 0) return 0;

        var changes = GetChangesForTransfer(from, to, cargoTypeId, amount);
        return TryApplyChanges(changes) ? amount : 0;
    }

    public bool CanExecuteRecipe(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
        => CanApplyChanges(GetChangesForRecipe(inventoryId, recipe));

    public bool TryExecuteRecipe(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
        => TryApplyChanges(GetChangesForRecipe(inventoryId, recipe));

    private static InventoryChange[] GetChangesForTransfer(Id<CargoInventory> from, Id<CargoInventory> to,
        Id<CargoType> cargoTypeId, int amount) =>
    [
        new(from, cargoTypeId, -amount),
        new(to, cargoTypeId, amount),
    ];

    private static InventoryChange[] GetChangesForRecipe(Id<CargoInventory> inventoryId, IndustryRecipe recipe) =>
    [
        ..recipe.Inputs.Select(input => new InventoryChange(inventoryId, input.CargoTypeId, -input.Amount)),
        ..recipe.Outputs.Select(output => new InventoryChange(inventoryId, output.CargoTypeId, output.Amount)),
    ];

    private bool CanApplyChanges(InventoryChange[] changes)
    {
        foreach (var change in changes)
        {
            if (change.Delta == 0) continue;

            if (change.Delta > 0)
            {
                if (!cargoInventoryService.CanAccept(change.InventoryId, change.CargoTypeId)) return false;
                if (cargoInventoryService.GetRemainingCapacityForType(change.InventoryId, change.CargoTypeId) < change.Delta) return false;
            }
            else
            {
                if (cargoInventoryService.GetAmount(change.InventoryId, change.CargoTypeId) + change.Delta < 0) return false;
            }
        }

        return true;
    }

    private bool TryApplyChanges(InventoryChange[] changes)
    {
        if (!CanApplyChanges(changes))
        {
            return false;
        }

        foreach (var change in changes)
        {
            if (change.Delta == 0) continue;
            if (!cargoInventoryService.TryUpdateExact(change.InventoryId, change.CargoTypeId, change.Delta))
            {
                logger.LogError("Transfer could not be completed atomically due to failed update: {Change}", change);
            }
        }

        return true;
    }
}
