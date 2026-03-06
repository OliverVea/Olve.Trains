namespace Olve.Trains.Scenes.GameLogic.Cargo;

public static class IndustryRecipeCatalog
{
    public static Id<IndustryRecipe> Forest { get; } = Id.FromName<IndustryRecipe>("recipe/forest");
    public static Id<IndustryRecipe> Mine { get; } = Id.FromName<IndustryRecipe>("recipe/mine");
    public static Id<IndustryRecipe> Sawmill { get; } = Id.FromName<IndustryRecipe>("recipe/sawmill");
}
