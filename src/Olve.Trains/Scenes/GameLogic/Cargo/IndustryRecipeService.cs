using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public class IndustryRecipeService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<IndustryRecipe> _recipes = entityStoreFactory.Create<IndustryRecipe>();

    public Id<IndustryRecipe> AddRecipe(IndustryRecipe recipe)
    {
        _recipes.TryAdd(recipe);
        return recipe.Id;
    }

    public bool TryGetRecipe(Id<IndustryRecipe> id, out IndustryRecipe recipe) => _recipes.TryGet(id, out recipe);

    public IEnumerable<IndustryRecipe> Recipes => _recipes.Values;
}
