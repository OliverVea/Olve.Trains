using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingBlueprintLibraryService(
    BuildingBlueprintService buildingBlueprintService,
    StationBlueprintService stationBlueprintService) : ISceneService
{
    public Result Load()
    {
        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Station, "Station", new TileFootprint(4, 2, 2));
        stationBlueprintService.SetProperties(BuildingBlueprintCatalog.Station, new StationProperties(Range: 5));

        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Residential, "Residential", new TileFootprint(1, 1, 1));

        return Result.Success();
    }

    public Result Unload()
    {
        stationBlueprintService.ClearProperties(BuildingBlueprintCatalog.Station);

        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Station);
        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Residential);

        return Result.Success();
    }
}
