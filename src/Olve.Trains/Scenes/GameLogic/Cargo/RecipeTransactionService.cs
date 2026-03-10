namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class RecipeTransactionService(CargoTransferService cargoTransferService)
{
    public bool CanExecute(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
    {
        return cargoTransferService.CanExecuteRecipe(inventoryId, recipe);
    }

    public bool TryExecute(Id<CargoInventory> inventoryId, IndustryRecipe recipe)
    {
        return cargoTransferService.TryExecuteRecipe(inventoryId, recipe);
    }
}
