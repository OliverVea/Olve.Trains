using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Depots;

public class DepotService(
    ILogger<DepotService> logger,
    DepotBlueprintService depotBlueprintService,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    TrackService trackService,
    GridService gridService)
{
    private readonly Dictionary<Id<Building>, Depot> _depots = [];
    private readonly Dictionary<Id<Track>, Depot> _depotsByTrack = [];

    public Event<Id<Building>> DepotCreated { get; } = new();
    public Event<Id<Building>> DepotDeleted { get; } = new();

    public Result<bool> CreateDepotForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        if (!depotBlueprintService.HasProperties(building.BlueprintId))
        {
            return false;
        }

        var direction = building.Position.CardinalDirection.RotateClockwise().ToVector3D();
        var origin = gridService.ToTileCenter(building.Position.BottomLeft);
        var start = origin - direction;
        var end = origin + direction * blueprint.Footprint.Width;

        TrackEndpoint trackStart = new(start, -direction);
        TrackEndpoint trackEnd = new(end, direction);

        if (trackService
                .AddTrack(trackStart, trackEnd)
                .TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add depot track for building '{0}'", buildingId);
        }

        Depot depot = new(buildingId, trackId);
        _depots[buildingId] = depot;
        _depotsByTrack[trackId] = depot;
        DepotCreated.Invoke(buildingId);

        logger.LogInformation("Created depot for building {BuildingId} with track {TrackId}", buildingId, trackId);

        return true;
    }

    public bool CanDeleteDepotForBuilding(Id<Building> buildingId)
    {
        return !_depots.TryGetValue(buildingId, out var depot) || trackService.CanDeleteTrack(depot.TrackId);
    }

    public Result DeleteDepotForBuilding(Id<Building> buildingId)
    {
        if (!_depots.TryGetValue(buildingId, out var depot))
        {
            return Result.Success();
        }

        if (!trackService.CanDeleteTrack(depot.TrackId))
        {
            return new ResultProblem("Cannot delete depot for building '{0}': its track is occupied", buildingId);
        }

        _depots.Remove(buildingId);
        _depotsByTrack.Remove(depot.TrackId);
        trackService.DeleteTrack(depot.TrackId);
        DepotDeleted.Invoke(buildingId);

        logger.LogInformation("Deleted depot for building {BuildingId}", buildingId);

        return Result.Success();
    }

    public bool TryGetDepot(Id<Building> buildingId, out Depot depot) =>
        _depots.TryGetValue(buildingId, out depot);

    public bool TryGetDepotByTrack(Id<Track> trackId, out Depot depot) =>
        _depotsByTrack.TryGetValue(trackId, out depot);
}
