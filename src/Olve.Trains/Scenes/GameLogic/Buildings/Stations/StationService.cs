using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Stations;

public class StationService(ILogger<StationService> logger, StationBlueprintService stationBlueprintService, BuildingService buildingService, BuildingBlueprintService buildingBlueprintService, TrackService trackService)
{
    private readonly Dictionary<Id<Building>, Station> _stations = [];

    public Event<Id<Building>> StationCreated { get; } = new();
    public Event<Id<Building>> StationDeleted { get; } = new();

    public Result<bool> CreateStationForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        if (!stationBlueprintService.HasProperties(building.BlueprintId))
        {
            return false;
        }

        var direction = building.Position.CardinalDirection.ToVector3D();
        var start = building.Position.BottomLeft.AsVector3D() - direction;
        var end = building.Position.BottomLeft.AsVector3D() + direction * blueprint.Footprint.Width;

        TrackEndpoint trackStart = new(start, -direction);
        TrackEndpoint trackEnd = new(end, direction);

        if (trackService
                .AddTrack(trackStart, trackEnd)
                .TryPickProblems(out var problems, out var trackId))
        {
            return problems.Prepend("Failed to add station track for building '{0}'", buildingId);
        }

        Station station = new(buildingId, trackId);
        _stations[buildingId] = station;
        StationCreated.Invoke(buildingId);

        logger.LogInformation("Created station for building {BuildingId} with track {TrackId}", buildingId, trackId);

        return true;
    }

    public Result DeleteStationForBuilding(Id<Building> buildingId)
    {
        if (!_stations.Remove(buildingId, out var station))
        {
            return Result.Success();
        }

        trackService.DeleteTrack(station.TrackId);
        StationDeleted.Invoke(buildingId);

        logger.LogInformation("Deleted station for building {BuildingId}", buildingId);

        return Result.Success();
    }

    public bool TryGetStation(Id<Building> buildingId, out Station station) =>
        _stations.TryGetValue(buildingId, out station);
}
