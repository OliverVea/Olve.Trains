using Microsoft.Extensions.Logging;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Industries;

public class IndustryService(
    ILogger<IndustryService> logger,
    BuildingService buildingService,
    IndustryBlueprintService industryBlueprintService,
    IndustryRecipeService industryRecipeService,
    CargoInventoryService cargoInventoryService,
    CargoTransferPolicyService cargoTransferPolicyService)
{
    private readonly Dictionary<Id<Building>, Industry> _industries = new();

    public Result<bool> CreateIndustryForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!industryBlueprintService.TryGetProperties(building.BlueprintId, out var properties))
        {
            return false;
        }

        if (!industryRecipeService.TryGetRecipe(properties.RecipeId, out var recipe))
        {
            return new ResultProblem("Recipe not found: '{0}'", properties.RecipeId);
        }

        var inventoryId = cargoInventoryService.CreateInventory(properties.Capacity, properties.AllowedTypes);

        foreach (var input in recipe.Inputs)
        {
            cargoTransferPolicyService.SetPolicy(inventoryId, input.CargoTypeId, TransferDirection.In);
        }

        foreach (var output in recipe.Outputs)
        {
            cargoTransferPolicyService.SetPolicy(inventoryId, output.CargoTypeId, TransferDirection.Out);
        }

        Industry industry = new(Id.New<Industry>(), buildingId, recipe.Id, inventoryId);
        _industries[buildingId] = industry;

        logger.LogInformation(
            "Created industry {IndustryId} for building {BuildingId} with recipe {RecipeName} and inventory {InventoryId}",
            industry.Id, buildingId, recipe.Name, inventoryId);

        return true;
    }

    public Result RemoveIndustryForBuilding(Id<Building> buildingId)
    {
        if (!_industries.Remove(buildingId, out var industry))
        {
            return Result.Success();
        }

        cargoTransferPolicyService.RemoveAllPolicies(industry.InventoryId);
        cargoInventoryService.RemoveInventory(industry.InventoryId);

        logger.LogInformation("Removed industry {IndustryId} for building {BuildingId}", industry.Id, buildingId);

        return Result.Success();
    }

    public IEnumerable<Industry> Industries => _industries.Values;

    public bool TryGetByBuilding(Id<Building> buildingId, out Industry industry) =>
        _industries.TryGetValue(buildingId, out industry);
}
