using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonBlueprintLibraryService(WagonBlueprintService wagonBlueprintService) : ISceneService
{
    public Result Load()
    {
        wagonBlueprintService.Register(new WagonBlueprint(
            WagonBlueprintCatalog.GoodsWagon,
            "Goods Wagon",
            Capacity: 10,
            AllowedTypes: null,
            Length: 1.0f));

        return Result.Success();
    }

    public Result Unload()
    {
        wagonBlueprintService.Remove(WagonBlueprintCatalog.GoodsWagon);
        return Result.Success();
    }
}
