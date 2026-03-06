using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Trains.Scenes.GameLogic.Buildings.Industries;
using Olve.Trains.Scenes.GameLogic.Buildings.Residences;
using Olve.Trains.Scenes.GameLogic.Buildings.Stations;
using Olve.Trains.Scenes.GameLogic.Cargo;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingBlueprintLibraryService(
    BuildingBlueprintService buildingBlueprintService,
    StationBlueprintService stationBlueprintService,
    ResidenceBlueprintService residenceBlueprintService,
    IndustryBlueprintService industryBlueprintService,
    BuildingMeshBlueprintService buildingMeshBlueprintService) : ISceneService
{
    public Result Load()
    {
        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Station, "Station", new TileFootprint(4, 2, 2));
        stationBlueprintService.SetProperties(BuildingBlueprintCatalog.Station, new StationProperties(Range: 5));
        buildingMeshBlueprintService.SetProperties(BuildingBlueprintCatalog.Station,
            new BuildingMeshProperties(Meshes.SM_Bld_Station_Small_01, Matrix4X4.CreateTranslation(0f, 0f, 0.1f)));

        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Residential, "Residential", new TileFootprint(1, 1, 1));
        residenceBlueprintService.SetProperties(BuildingBlueprintCatalog.Residential, new ResidenceProperties(Capacity: 4));
        buildingMeshBlueprintService.SetProperties(BuildingBlueprintCatalog.Residential,
            new BuildingMeshProperties(Meshes.apartment_small_mesh, Matrix4X4<float>.Identity));

        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Forest, "Forest", new TileFootprint(2, 1, 2));
        industryBlueprintService.SetProperties(BuildingBlueprintCatalog.Forest, new IndustryProperties(IndustryRecipeCatalog.Forest));

        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Mine, "Mine", new TileFootprint(2, 1, 2));
        industryBlueprintService.SetProperties(BuildingBlueprintCatalog.Mine, new IndustryProperties(IndustryRecipeCatalog.Mine));

        buildingBlueprintService.AddBlueprint(BuildingBlueprintCatalog.Sawmill, "Sawmill", new TileFootprint(3, 1, 2));
        industryBlueprintService.SetProperties(BuildingBlueprintCatalog.Sawmill, new IndustryProperties(IndustryRecipeCatalog.Sawmill));

        return Result.Success();
    }

    public Result Unload()
    {
        stationBlueprintService.ClearProperties(BuildingBlueprintCatalog.Station);
        residenceBlueprintService.ClearProperties(BuildingBlueprintCatalog.Residential);
        industryBlueprintService.ClearProperties(BuildingBlueprintCatalog.Forest);
        industryBlueprintService.ClearProperties(BuildingBlueprintCatalog.Mine);
        industryBlueprintService.ClearProperties(BuildingBlueprintCatalog.Sawmill);
        buildingMeshBlueprintService.ClearProperties(BuildingBlueprintCatalog.Station);
        buildingMeshBlueprintService.ClearProperties(BuildingBlueprintCatalog.Residential);

        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Station);
        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Residential);
        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Forest);
        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Mine);
        buildingBlueprintService.DeleteBlueprint(BuildingBlueprintCatalog.Sawmill);

        return Result.Success();
    }
}
