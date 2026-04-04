using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public class WagonBlueprintLibraryService(WagonBlueprintService wagonBlueprintService) : ISceneService
{
    public Result Load()
    {
        if (wagonBlueprintService.Register(new WagonBlueprint(
                WagonBlueprintCatalog.GoodsWagon,
                "Goods Wagon",
                Capacity: 10,
                AllowedTypes: null,
                Length: 16.2f / WorldScale.TileSizeInMeters))
            .TryPickProblems(out var problems))
        {
            return problems;
        }

        return Result.Success();
    }

    public Result Unload()
    {
        wagonBlueprintService.Remove(WagonBlueprintCatalog.GoodsWagon);
        return Result.Success();
    }
}
