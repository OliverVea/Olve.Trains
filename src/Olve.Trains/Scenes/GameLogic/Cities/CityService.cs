using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Cities;

public class CityService(
    ILogger<CityService> logger,
    BuildingService buildingService,
    ResidenceBlueprintService residenceBlueprintService,
    CargoInventoryService cargoInventoryService,
    CargoTransferPolicyService cargoTransferPolicyService)
{
    private const int CityInventoryCapacity = 1000;

    private static readonly ImmutableDictionary<Id<CargoType>, int> AllowedTypes =
        ImmutableDictionary<Id<CargoType>, int>.Empty
            .Add(CargoTypeCatalog.Planks, CityInventoryCapacity);

    private readonly Dictionary<Id<City>, City> _cities = new();
    private readonly Dictionary<Id<Building>, Residence> _residencesByBuilding = new();

    public IEnumerable<City> Cities => _cities.Values;
    public IEnumerable<Residence> Residences => _residencesByBuilding.Values;

    public bool TryGetCity(Id<City> cityId, out City city) => _cities.TryGetValue(cityId, out city);

    public bool TryGetResidenceByBuilding(Id<Building> buildingId, out Residence residence) =>
        _residencesByBuilding.TryGetValue(buildingId, out residence);

    public Result<bool> CreateResidenceForBuilding(Id<Building> buildingId)
    {
        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!residenceBlueprintService.HasProperties(building.BlueprintId))
        {
            return false;
        }

        var city = _cities.Values.FirstOrDefault();
        if (city.Id == default)
        {
            city = CreateCity();
        }

        Residence residence = new(Id.New<Residence>(), buildingId, city.Id);
        _residencesByBuilding[buildingId] = residence;

        logger.LogInformation(
            "Created residence {ResidenceId} for building {BuildingId} attached to city {CityId}",
            residence.Id, buildingId, city.Id);

        return true;
    }

    public Result RemoveResidenceForBuilding(Id<Building> buildingId)
    {
        if (!_residencesByBuilding.Remove(buildingId, out var residence))
        {
            return Result.Success();
        }

        logger.LogInformation(
            "Removed residence {ResidenceId} for building {BuildingId}",
            residence.Id, buildingId);

        return Result.Success();
    }

    private City CreateCity()
    {
        var inventoryId = cargoInventoryService.CreateInventory(CityInventoryCapacity, AllowedTypes);
        cargoTransferPolicyService.SetPolicy(inventoryId, CargoTypeCatalog.Planks, TransferDirection.In);

        City city = new(Id.New<City>(), inventoryId);
        _cities[city.Id] = city;

        logger.LogInformation("Created city {CityId} with inventory {InventoryId}", city.Id, inventoryId);

        return city;
    }
}
