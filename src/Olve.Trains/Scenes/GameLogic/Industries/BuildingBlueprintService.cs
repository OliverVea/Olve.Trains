using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingBlueprintService
{
    private readonly EntityStore<BuildingBlueprint> _blueprints = new();
    public Event<Id<BuildingBlueprint>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<Id<BuildingBlueprint>> OnBlueprintRemoved => _blueprints.OnRemoved;

    public Id<BuildingBlueprint> AddBlueprint(string description, TileFootprint footprint, BuildingType buildingType)
    {
        BuildingBlueprint blueprint = new(Id.New<BuildingBlueprint>(), description, footprint, buildingType);
        _blueprints.TryAdd(blueprint);
        return blueprint.Id;
    }

    public DeletionResult DeleteBlueprint(Id<BuildingBlueprint> blueprintId) => _blueprints.Remove(blueprintId);
    public bool TryGetBlueprint(Id<BuildingBlueprint> blueprintId, out BuildingBlueprint blueprint) => _blueprints.TryGet(blueprintId, out blueprint);
}