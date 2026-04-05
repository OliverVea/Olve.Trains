using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Money;

public static class MoneyConstants
{
    public const int StartingBalance = 10_000;

    public const int TrackCostPerMeter = 50;
    public const int TrainCost = 500;
    public const int WagonCost = 200;

    public static int GetBuildingCost(Id<BuildingBlueprint> blueprintId)
    {
        if (blueprintId == BuildingBlueprintCatalog.Station) return 800;
        if (blueprintId == BuildingBlueprintCatalog.Depot) return 600;
        if (blueprintId == BuildingBlueprintCatalog.Residential) return 300;
        if (blueprintId == BuildingBlueprintCatalog.Forest) return 400;
        if (blueprintId == BuildingBlueprintCatalog.Mine) return 500;
        if (blueprintId == BuildingBlueprintCatalog.Sawmill) return 500;
        return 100;
    }

    public static int GetCargoDeliveryValue(Id<CargoType> cargoTypeId, int amount)
    {
        var perUnit = 10;
        if (cargoTypeId == CargoTypeCatalog.Wood) perUnit = 10;
        else if (cargoTypeId == CargoTypeCatalog.Coal) perUnit = 15;
        else if (cargoTypeId == CargoTypeCatalog.Planks) perUnit = 25;
        return perUnit * amount;
    }
}
