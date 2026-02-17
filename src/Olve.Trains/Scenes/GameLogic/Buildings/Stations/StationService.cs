using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Stations;

public class StationService(ILogger<StationService> logger, StationBlueprintService stationBlueprintService, BuildingService buildingService, BuildingBlueprintService buildingBlueprintService, TrackService trackService)
{
    private readonly Dictionary<Id<Building>, Station> _stations = [];

    public Event<Id<Building>> StationCreated { get; } = new();

    public Result<bool> CreateStationForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("TODO");
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("TODO");
        }

        if (!stationBlueprintService.HasProperties(building.BlueprintId))
        {
            logger.LogDebug("TODO: NOT A STATION");
            return false;
        }

        var direction = building.Position.CardinalDirection.ToVector3D();
        var start = building.Position.BottomLeft.AsVector3D() - direction;
        var end = building.Position.BottomLeft.AsVector3D() + direction * blueprint.Footprint.Width;

        TrackEndpoint trackStart = new(start, -direction);
        TrackEndpoint trackEnd = new(end, direction);

        if (!trackService
                .AddTrack(trackStart, trackEnd)
                .TryPickProblems(out var problems, out var trackId))
        {
            return new ResultProblem("TODO");
        }

        Station station = new(buildingId, trackId);
        _stations[buildingId] = station;
        StationCreated.Invoke(buildingId);

        return true;
    }
}