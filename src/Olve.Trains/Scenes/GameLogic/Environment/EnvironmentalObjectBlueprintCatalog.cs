namespace Olve.Trains.Scenes.GameLogic.Environment;

public static class EnvironmentalObjectBlueprintCatalog
{
    public static Id<EnvironmentalObjectBlueprint> Tree { get; } = Id.FromName<EnvironmentalObjectBlueprint>("catalog/tree");
    public static Id<EnvironmentalObjectBlueprint> Rock { get; } = Id.FromName<EnvironmentalObjectBlueprint>("catalog/rock");
    public static Id<EnvironmentalObjectBlueprint> Grass { get; } = Id.FromName<EnvironmentalObjectBlueprint>("catalog/grass");
}
