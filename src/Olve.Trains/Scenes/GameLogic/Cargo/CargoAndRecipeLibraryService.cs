using System.Collections.Immutable;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class CargoAndRecipeLibraryService(
    CargoTypeService cargoTypeService,
    IndustryRecipeService industryRecipeService) : ISceneService
{
    public Result Load()
    {
        cargoTypeService.AddCargoType(CargoTypeCatalog.Wood, "Wood");
        cargoTypeService.AddCargoType(CargoTypeCatalog.Coal, "Coal");
        cargoTypeService.AddCargoType(CargoTypeCatalog.Planks, "Planks");

        industryRecipeService.AddRecipe(new IndustryRecipe(
            IndustryRecipeCatalog.Forest,
            "Forest",
            Inputs: [],
            Outputs: [new CargoAmount(CargoTypeCatalog.Wood, 1)]));

        industryRecipeService.AddRecipe(new IndustryRecipe(
            IndustryRecipeCatalog.Mine,
            "Mine",
            Inputs: [],
            Outputs: [new CargoAmount(CargoTypeCatalog.Coal, 1)]));

        industryRecipeService.AddRecipe(new IndustryRecipe(
            IndustryRecipeCatalog.Sawmill,
            "Sawmill",
            Inputs: [new CargoAmount(CargoTypeCatalog.Wood, 1)],
            Outputs: [new CargoAmount(CargoTypeCatalog.Planks, 1)]));

        return Result.Success();
    }

    public Result Unload()
    {
        return Result.Success();
    }
}
