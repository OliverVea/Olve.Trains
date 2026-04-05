using Microsoft.Extensions.Logging;
using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Shared.Telemetry;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingService
{
    private readonly ILogger<BuildingService> _logger;
    private readonly BuildingBlueprintService _blueprintService;
    private readonly MoneyService _moneyService;
    private readonly EntityStore<Building> _buildings;
    private readonly EntityStoreIndex<Building, Id<BuildingBlueprint>> _buildingsByBlueprint;

    public BuildingService(ILogger<BuildingService> logger, BuildingBlueprintService blueprintService, MoneyService moneyService, EntityStoreFactory entityStoreFactory)
    {
        _logger = logger;
        _blueprintService = blueprintService;
        _moneyService = moneyService;
        _buildings = entityStoreFactory.Create<Building>();
        _buildingsByBlueprint = _buildings.CreateIndex(x => x.BlueprintId);
    }

    public Event<Id<Building>> OnBuildingAdded => _buildings.OnAdded;
    public Event<Id<Building>> OnBuildingRemoved => _buildings.OnRemoved;

    public Result<Id<Building>> AddBuilding(Id<BuildingBlueprint> blueprintId, BuildingPosition position)
    {
        if (!_blueprintService.TryGetBlueprint(blueprintId, out var blueprint))
        {
            return new ResultProblem("Building blueprint '{0}' not found", blueprintId);
        }

        var cost = MoneyConstants.GetBuildingCost(blueprintId);
        if (!_moneyService.TryCharge(cost, $"place {blueprint.Description}"))
        {
            return new ResultProblem("Cannot afford {0}: need {1}, have {2}", blueprint.Description, cost, _moneyService.Balance);
        }

        Building building = new(Id.New<Building>(), blueprintId, position);

        if (!_buildings.TryAdd(building))
        {
            return new ResultProblem("Failed to add building '{0}' with blueprint '{1}'", building.Id, blueprintId);
        }

        _logger.LogInformation(
            "Added building {BuildingId} type={Type} pos={X},{Y},{Z} dir={Direction}",
            building.Id, blueprint.Description,
            position.BottomLeft.X, position.BottomLeft.Y, position.BottomLeft.Z,
            position.CardinalDirection);
        if (EngineMetrics.IsEnabled) GameMetrics.BuildingCount.Add(1);

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
            if (EngineMetrics.IsEnabled) GameMetrics.BuildingCount.Add(-1);
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
    public IEnumerable<Id<Building>> BuildingIds => _buildings.Keys;
    public IEnumerable<Building> Buildings => _buildings.Values;
    public IReadOnlyCollection<Id<Building>> GetBuildingsWithBlueprint(Id<BuildingBlueprint> blueprintId) =>
        _buildingsByBlueprint.GetForKey(blueprintId);
}