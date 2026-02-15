using Olve.Engine3D.Scenes;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Industries;

public class BuildingBlueprintLibraryService(BuildingBlueprintService blueprintService)  : ISceneService
{
    private Id<BuildingBlueprint>? _stationBlueprint;
    public Id<BuildingBlueprint> StationBlueprint => _stationBlueprint ?? throw new NotInitializedException<Id<BuildingBlueprint>>();

    public Result Load()
    {
        _stationBlueprint = blueprintService.AddBlueprint("Station", new TileFootprint(6, 2, 2), BuildingType.Station);
        return Result.Success();
    }

    public Result Unload()
    {
        if (_stationBlueprint != null)
        {
            blueprintService.DeleteBlueprint(_stationBlueprint.Value);
            _stationBlueprint = null;
        }

        return Result.Success();
    }
}