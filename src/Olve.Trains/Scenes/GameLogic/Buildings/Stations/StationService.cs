using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Engine3D.Systems;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Cities;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Stations;

public class StationService(ILogger<StationService> logger, StationBlueprintService stationBlueprintService, BuildingService buildingService, BuildingBlueprintService buildingBlueprintService, TrackService trackService, GridService gridService, IndustryService industryService, CityService cityService)
{
    private readonly Dictionary<Id<Building>, Station> _stations = [];
    private readonly Dictionary<Id<Track>, Station> _stationsByTrack = [];

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
            return problems.Prepend("Failed to add station track for building '{0}'", buildingId);
        }

        Station station = new(buildingId, trackId);
        _stations[buildingId] = station;
        _stationsByTrack[trackId] = station;
        StationCreated.Invoke(buildingId);

        logger.LogInformation("Created station for building {BuildingId} with track {TrackId}", buildingId, trackId);

        return true;
    }

    public bool CanDeleteStationForBuilding(Id<Building> buildingId)
    {
        return !_stations.TryGetValue(buildingId, out var station) || trackService.CanDeleteTrack(station.TrackId);
    }

    public Result DeleteStationForBuilding(Id<Building> buildingId)
    {
        if (!_stations.TryGetValue(buildingId, out var station))
        {
            return Result.Success();
        }

        if (!trackService.CanDeleteTrack(station.TrackId))
        {
            return new ResultProblem("Cannot delete station for building '{0}': its track is occupied", buildingId);
        }

        _stations.Remove(buildingId);
        _stationsByTrack.Remove(station.TrackId);
        trackService.DeleteTrack(station.TrackId);
        StationDeleted.Invoke(buildingId);

        logger.LogInformation("Deleted station for building {BuildingId}", buildingId);

        return Result.Success();
    }

    public bool TryGetStation(Id<Building> buildingId, out Station station) =>
        _stations.TryGetValue(buildingId, out station);

    public bool TryGetStationByTrack(Id<Track> trackId, out Station station) =>
        _stationsByTrack.TryGetValue(trackId, out station);

    public IEnumerable<Industry> GetNearbyIndustries(Id<Building> stationBuildingId)
    {
        if (!buildingService.TryGetBuilding(stationBuildingId, out var stationBuilding)) yield break;
        if (!stationBlueprintService.TryGetProperties(stationBuilding.BlueprintId, out var stationProps)) yield break;

        foreach (var building in buildingService.Buildings)
        {
            if (!industryService.TryGetByBuilding(building.Id, out var industry)) continue;

            var distance = TileDistance(stationBuilding.Position.BottomLeft, building.Position.BottomLeft);
            if (distance > stationProps.Range) continue;

            yield return industry;
        }
    }

    public IEnumerable<City> GetNearbyCities(Id<Building> stationBuildingId)
    {
        if (!buildingService.TryGetBuilding(stationBuildingId, out var stationBuilding)) yield break;
        if (!stationBlueprintService.TryGetProperties(stationBuilding.BlueprintId, out var stationProps)) yield break;

        var seenCities = new HashSet<Id<City>>();

        foreach (var building in buildingService.Buildings)
        {
            if (!cityService.TryGetResidenceByBuilding(building.Id, out var residence)) continue;

            var distance = TileDistance(stationBuilding.Position.BottomLeft, building.Position.BottomLeft);
            if (distance > stationProps.Range) continue;

            if (!seenCities.Add(residence.CityId)) continue;
            if (!cityService.TryGetCity(residence.CityId, out var city)) continue;

            yield return city;
        }
    }

    internal static int TileDistance(TilePosition a, TilePosition b)
    {
        var dx = Math.Abs(a.X - b.X);
        var dz = Math.Abs(a.Z - b.Z);
        return Math.Max(dx, dz);
    }
}
