namespace Olve.Trains.Scenes.GameLogic.Buildings;

public static class BuildingBlueprintCatalog
{
    public static Id<BuildingBlueprint> Station { get; } = Id.FromName<BuildingBlueprint>("catalog/station");
    public static Id<BuildingBlueprint> Residential { get; } = Id.FromName<BuildingBlueprint>("catalog/residential");
    public static Id<BuildingBlueprint> Forest { get; } = Id.FromName<BuildingBlueprint>("catalog/forest");
    public static Id<BuildingBlueprint> Mine { get; } = Id.FromName<BuildingBlueprint>("catalog/mine");
    public static Id<BuildingBlueprint> Sawmill { get; } = Id.FromName<BuildingBlueprint>("catalog/sawmill");
}
