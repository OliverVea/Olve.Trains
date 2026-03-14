using Microsoft.Extensions.Logging;
using Olve.Engine3D.Assets;
using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public class EnvironmentalObjectBlueprintService(ILogger<EnvironmentalObjectBlueprintService> logger, EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<EnvironmentalObjectBlueprint> _blueprints = entityStoreFactory.Create<EnvironmentalObjectBlueprint>();

    public Event<Id<EnvironmentalObjectBlueprint>> OnBlueprintAdded => _blueprints.OnAdded;
    public Event<Id<EnvironmentalObjectBlueprint>> OnBlueprintRemoved => _blueprints.OnRemoved;

    public Result AddBlueprint(Id<EnvironmentalObjectBlueprint> id, string description, AssetPath<MeshData>? meshPath = null)
    {
        EnvironmentalObjectBlueprint blueprint = new(id, description, meshPath);

        if (!_blueprints.TryAdd(blueprint))
        {
            return new ResultProblem("Failed to add environmental object blueprint '{0}' '{1}'", blueprint.Id, description);
        }

        logger.LogDebug("Added environmental object blueprint {BlueprintId} '{Description}'", blueprint.Id, description);
        return Result.Success();
    }

    public DeletionResult DeleteBlueprint(Id<EnvironmentalObjectBlueprint> blueprintId)
    {
        var result = _blueprints.Remove(blueprintId);
        logger.LogDebug("Deleted environmental object blueprint {BlueprintId}", blueprintId);
        return result;
    }

    public bool TryGetBlueprint(Id<EnvironmentalObjectBlueprint> blueprintId, out EnvironmentalObjectBlueprint blueprint)
        => _blueprints.TryGet(blueprintId, out blueprint);
}
