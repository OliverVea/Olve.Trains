using Microsoft.Extensions.Logging;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingService
{
    private readonly ILogger<BuildingService> _logger;
    private readonly EntityStore<Building> _buildings;
    private readonly EntityStoreIndex<Building, Id<BuildingBlueprint>> _buildingsByBlueprint;

    public BuildingService(ILogger<BuildingService> logger, EntityStoreFactory entityStoreFactory)
    {
        _logger = logger;
        _buildings = entityStoreFactory.Create<Building>();
        _buildingsByBlueprint = _buildings.CreateIndex(x => x.BlueprintId);
    }

    public Event<Id<Building>> OnBuildingAdded => _buildings.OnAdded;
    public Event<Id<Building>> OnBuildingRemoved => _buildings.OnRemoved;

    public Id<Building> AddBuilding(Id<BuildingBlueprint> blueprintId, BuildingPosition position)
    {
        Building building = new(Id.New<Building>(), blueprintId, position);

        if (!_buildings.TryAdd(building))
        {
            _logger.LogWarning("Failed to add building {BuildingId} with blueprint {BlueprintId} at {Origin}", building.Id, blueprintId, position);
        }
        else
        {
            _logger.LogInformation("Added building {BuildingId} with blueprint {BlueprintId} at {Origin}", building.Id, blueprintId, position);
        }

        return building.Id;
    }

    public DeletionResult DeleteBuilding(Building building) => DeleteBuilding(building.Id);

    public DeletionResult DeleteBuilding(Id<Building> buildingId)
    {
        var result = _buildings.Remove(buildingId);
        if (result.WasNotFound)
        {
            _logger.LogWarning("Tried to delete building {BuildingId} but it was not found", buildingId);
        }
        else
        {
            _logger.LogInformation("Deleted building {BuildingId}", buildingId);
        }

        return result;
    }

    public Result DeleteBuildingsWithBlueprint(Id<BuildingBlueprint> blueprintId)
    {
        foreach (var buildingId in _buildingsByBlueprint.GetForKey(blueprintId).ToArray())
        {
            var result = DeleteBuilding(buildingId);
            if (result.WasNotFound)
            {
                _logger.LogWarning("Building with id '{BuildingId}' was not found while deleting all buildings with blueprint '{BlueprintId}'", buildingId, blueprintId);
            }

            if (result.TryPickProblems(out var problems))
            {
                _logger.Log(problems.Prepend("Failed to delete building with id '{0}' while deleting all buildings with blueprint '{1}'", buildingId, blueprintId));
            }
        }

        return Result.Success();
    }
    public bool TryGetBuilding(Id<Building> buildingId, out Building building)
        => _buildings.TryGet(buildingId, out building);
    public IEnumerable<Building> GetAllBuildings()
        => _buildings.Where(_ => true);
    public IReadOnlyCollection<Id<Building>> GetBuildingsWithBlueprint(Id<BuildingBlueprint> blueprintId) =>
        _buildingsByBlueprint.GetForKey(blueprintId);
}