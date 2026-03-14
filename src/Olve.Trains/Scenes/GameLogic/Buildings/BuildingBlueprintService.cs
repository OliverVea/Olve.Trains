using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingBlueprintService(ILogger<BuildingBlueprintService> logger, EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<BuildingBlueprint> _blueprints = entityStoreFactory.Create<BuildingBlueprint>();
    public Event<Id<BuildingBlueprint>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<Id<BuildingBlueprint>> OnBlueprintRemoved => _blueprints.OnRemoved;

    public Result AddBlueprint(Id<BuildingBlueprint> id, string description, TileFootprint footprint)
    {
        BuildingBlueprint blueprint = new(id, description, footprint);

        if (!_blueprints.TryAdd(blueprint))
        {
            return new ResultProblem("Failed to add building blueprint '{0}' '{1}'", blueprint.Id, description);
        }

        logger.LogDebug("Added blueprint {BlueprintId} '{Description}' ({Footprint})", blueprint.Id, description, footprint);
        return Result.Success();
    }

    public DeletionResult DeleteBlueprint(Id<BuildingBlueprint> blueprintId)
    {
        var result = _blueprints.Remove(blueprintId);
        logger.LogDebug("Deleted blueprint {BlueprintId}", blueprintId);
        return result;
    }

    public bool TryGetBlueprint(Id<BuildingBlueprint> blueprintId, out BuildingBlueprint blueprint) => _blueprints.TryGet(blueprintId, out blueprint);
}