using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingBlueprintService(ILogger<BuildingBlueprintService> logger, EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<BuildingBlueprint> _blueprints = entityStoreFactory.Create<BuildingBlueprint>();
    public Event<Id<BuildingBlueprint>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<Id<BuildingBlueprint>> OnBlueprintRemoved => _blueprints.OnRemoved;

    public Id<BuildingBlueprint> AddBlueprint(string description, TileFootprint footprint, BuildingType buildingType)
    {
        BuildingBlueprint blueprint = new(Id.New<BuildingBlueprint>(), description, footprint, buildingType);

        if (!_blueprints.TryAdd(blueprint))
        {
            logger.LogWarning("Failed to add blueprint {BlueprintId} '{Description}'", blueprint.Id, description);
        }
        else
        {
            logger.LogDebug("Added blueprint {BlueprintId} '{Description}' ({BuildingType}, {Footprint})", blueprint.Id, description, buildingType, footprint);
        }

        return blueprint.Id;
    }

    public DeletionResult DeleteBlueprint(Id<BuildingBlueprint> blueprintId)
    {
        var result = _blueprints.Remove(blueprintId);
        logger.LogDebug("Deleted blueprint {BlueprintId}", blueprintId);
        return result;
    }

    public bool TryGetBlueprint(Id<BuildingBlueprint> blueprintId, out BuildingBlueprint blueprint) => _blueprints.TryGet(blueprintId, out blueprint);
}