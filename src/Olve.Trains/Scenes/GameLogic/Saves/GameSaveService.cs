using System.Linq;
using Olve.Engine3D.Time;
using Olve.Trains.Saves;
using Olve.Trains.Scenes.GameLogic.Camera;
using Olve.Trains.Scenes.GameLogic.Environment;
using Olve.Trains.Scenes.GameLogic.Money;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Trains.Scenes.GameLogic.Time;

namespace Olve.Trains.Scenes.GameLogic.Saves;

/// <summary>
/// Snapshots the live game-logic services into a <see cref="SaveFile"/>. The materialized result is the
/// save format's source of truth — terrain heightmap and environmental objects are captured in full,
/// never as a seed to regenerate from.
/// </summary>
public sealed class GameSaveService(
    MoneyService moneyService,
    DayTimeManager dayTimeManager,
    DayTimeSteppingService dayTimeSteppingService,
    TerrainService terrainService,
    EnvironmentalObjectService environmentalObjectService,
    CameraSceneService cameraSceneService)
{
    /// <summary>Captures current root game state as a versioned save snapshot.</summary>
    public SaveFile CaptureSaveFile() => new()
    {
        Money = moneyService.Balance,
        Time = new SaveTime
        {
            DayStartHours = dayTimeSteppingService.DayStart.Value,
            DayDurationSeconds = dayTimeManager.DayLength.TotalSeconds,
            CurrentGameHours = dayTimeManager.TotalGameHours,
        },
        Terrain = terrainService.Terrain.Heightmap.ToSaveTerrain(),
        Environment = new SaveEnvironment
        {
            Objects = environmentalObjectService.Objects
                .Select(o => o.ToSaveEnvironmentalObject())
                .ToList(),
        },
        Camera = new SaveCamera
        {
            OrthographicSize = cameraSceneService.Camera.Projection.OrthographicSize,
        },
    };
}
