namespace Olve.Trains.Scenes.GameLogic.Buildings;

public static class BuildingBlueprintCatalog
{
    public static Id<BuildingBlueprint> Station { get; } = Id.FromName<BuildingBlueprint>("catalog/station");
    public static Id<BuildingBlueprint> Residential { get; } = Id.FromName<BuildingBlueprint>("catalog/residential");
}
