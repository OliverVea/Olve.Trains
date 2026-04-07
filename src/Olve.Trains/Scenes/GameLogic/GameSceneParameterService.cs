using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;

namespace Olve.Trains.Scenes.GameLogic;

public class GameSceneParameterService(
    MoneyService moneyService,
    TerrainService terrainService,
    CameraSceneService cameraSceneService,
    DayTimeSteppingService dayTimeSteppingService) : ISceneParameterService<GameSceneArguments>
{
    public Result LoadParameters(GameSceneArguments parameters)
    {
        moneyService.Balance = parameters.StartingMoney;

        terrainService.HeightmapOverride = parameters.Heightmap ?? GameSceneArguments.DefaultHeightmap();
        terrainService.TreeSeed = parameters.TreeSeed;
        terrainService.TreeSpawnProbability = parameters.TreeSpawnProbability;

        cameraSceneService.InitialOrthographicSize = parameters.CameraOrthographicSize;

        dayTimeSteppingService.DayStartOverride = parameters.DayStart ?? new DayTime(5, 30);
        dayTimeSteppingService.DayDurationOverride = parameters.DayDuration ?? TimeSpan.FromMinutes(15);

        return Result.Success();
    }
}
