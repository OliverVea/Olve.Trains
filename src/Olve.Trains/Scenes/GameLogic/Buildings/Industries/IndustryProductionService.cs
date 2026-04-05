using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Trains.Scenes.GameLogic.Resources;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Industries;

public class IndustryProductionService(
    IndustryService industryService,
    IndustryRecipeService industryRecipeService,
    IndustryBlueprintService industryBlueprintService,
    BuildingService buildingService,
    RecipeTransactionService recipeTransactionService,
    ResourceOwnershipService resourceOwnershipService,
    DeltaTimeService deltaTimeService) : ISceneService
{
    private readonly Dictionary<(Id<Industry>, Id<IndustryRecipe>), TimeSpan> _accumulators = new();

    public Result Update()
    {
        var deltaTime = deltaTimeService.ScaledDeltaTime;

        foreach (var industry in industryService.Industries)
        {
            if (!industryRecipeService.TryGetRecipe(industry.RecipeId, out var recipe))
                continue;

            var key = (industry.Id, recipe.Id);

            if (!recipeTransactionService.CanExecute(industry.InventoryId, recipe))
            {
                _accumulators[key] = TimeSpan.Zero;
                continue;
            }

            var effectiveDelta = deltaTime;

            if (IsExtractiveIndustry(industry))
            {
                var productivity = resourceOwnershipService.GetProductivity(industry.Id);
                if (productivity <= 0f)
                {
                    _accumulators[key] = TimeSpan.Zero;
                    continue;
                }

                effectiveDelta *= productivity;
            }

            var accumulated = _accumulators.GetValueOrDefault(key) + effectiveDelta;

            if (accumulated >= recipe.ProductionInterval)
            {
                accumulated -= recipe.ProductionInterval;
                recipeTransactionService.TryExecute(industry.InventoryId, recipe);
            }

            _accumulators[key] = accumulated;
        }

        return Result.Success();
    }

    private bool IsExtractiveIndustry(Industry industry)
    {
        if (!buildingService.TryGetBuilding(industry.BuildingId, out var building)) return false;
        if (!industryBlueprintService.TryGetProperties(building.BlueprintId, out var props)) return false;
        return props.RequiredResourceType is not null;
    }
}
