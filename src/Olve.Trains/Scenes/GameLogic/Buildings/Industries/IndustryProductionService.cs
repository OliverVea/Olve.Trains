using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Industries;

public class IndustryProductionService(
    IndustryService industryService,
    IndustryRecipeService industryRecipeService,
    RecipeTransactionService recipeTransactionService) : ISceneService
{
    private readonly Dictionary<(Id<Industry>, Id<IndustryRecipe>), TimeSpan> _accumulators = new();

    public Result Update(TimeSpan deltaTime)
    {
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

            var accumulated = _accumulators.GetValueOrDefault(key) + deltaTime;

            if (accumulated >= recipe.ProductionInterval)
            {
                accumulated -= recipe.ProductionInterval;
                recipeTransactionService.TryExecute(industry.InventoryId, recipe);
            }

            _accumulators[key] = accumulated;
        }

        return Result.Success();
    }
}
