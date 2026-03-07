namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class RecipeTransactionService(CargoInventoryService cargoInventoryService)
{
    public bool CanExecute(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
    {
        foreach (var input in recipe.Inputs)
        {
            if (cargoInventoryService.GetAmount(inventoryId, input.CargoTypeId) < input.Amount)
                return false;
        }

        foreach (var output in recipe.Outputs)
        {
            if (cargoInventoryService.GetRemainingCapacityForType(inventoryId, output.CargoTypeId) < output.Amount)
                return false;
        }

        return true;
    }

    public bool TryExecute(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
    {
        if (!CanExecute(inventoryId, recipe))
            return false;

        foreach (var input in recipe.Inputs)
        {
            cargoInventoryService.TryUpdateExact(inventoryId, input.CargoTypeId, -input.Amount, inventoryId);
        }

        foreach (var output in recipe.Outputs)
        {
            cargoInventoryService.TryUpdateExact(inventoryId, output.CargoTypeId, output.Amount, inventoryId);
        }

        return true;
    }
}
