using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingService(ILogger<BuildingService> logger)
{
    private readonly EntityStore<Building> _buildings = new();
    public Event<Id<Building>> OnBuildingAdded => _buildings.OnAdded;
    public Event<Id<Building>> OnBuildingRemoved => _buildings.OnRemoved;

    public Id<Building> AddBuilding(Id<BuildingBlueprint> blueprintId, BuildingPosition position)
    {
        Building building = new(Id.New<Building>(), blueprintId, position);

        if (!_buildings.TryAdd(building))
        {
            logger.LogWarning("Failed to add building {BuildingId} with blueprint {BlueprintId} at {Origin}", building.Id, blueprintId, position);
        }
        else
        {
            logger.LogInformation("Added building {BuildingId} with blueprint {BlueprintId} at {Origin}", building.Id, blueprintId, position);
        }

        return building.Id;
    }

    public DeletionResult DeleteBuilding(Building building) => DeleteBuilding(building.Id);

    public DeletionResult DeleteBuilding(Id<Building> buildingId)
    {
        var result = _buildings.Remove(buildingId);
        if (result.WasNotFound)
        {
            logger.LogWarning("Tried to delete building {BuildingId} but it was not found", buildingId);
        }
        else
        {
            logger.LogInformation("Deleted building {BuildingId}", buildingId);
        }

        return result;
    }

    public Result DeleteBuildingsWithBlueprint(Id<BuildingBlueprint> blueprintId)
    {
        var toDelete = _buildings
            .Where(x => x.BlueprintId == blueprintId)
            .ToArray();

        foreach (var building in toDelete)
        {
            var result = DeleteBuilding(building.Id);
            if (result.WasNotFound)
            {
                logger.LogWarning("Building with id '{BuildingId}' was not found while deleting all buildings with blueprint '{BlueprintId}'", building.Id, blueprintId);
            }

            if (result.TryPickProblems(out var problems))
            {
                logger.Log(problems.Prepend("Failed to delete building with id '{0}' while deleting all buildings with blueprint '{1}'",  building.Id, blueprintId));
            }
        }

        return Result.Success();
    }
    public bool TryGetBuilding(Id<Building> buildingId, out Building building) => _buildings.TryGet(buildingId, out building);
}