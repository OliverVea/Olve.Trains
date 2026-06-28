using Olve.Engine3D.Time;
using Olve.Paths;
using Olve.Trains.Saves;
using Olve.Trains.Scenes.GameLogic;
using Olve.Trains.Scenes.GameLogic.Environment;

namespace Olve.Trains.Scenes.Loading;

/// <summary>
/// Reads a save file and reshapes it into <see cref="GameSceneArguments"/> — the single contract both new
/// and loaded games flow through. Pure CPU work (file read + DTO→domain mapping), safe to run on the
/// LoadingScene's background thread before the game scene is prepared.
/// </summary>
public sealed class GameLoadService(SaveFileStore saveFileStore)
{
    public Result<GameSceneArguments> BuildGameSceneArguments(IPath savePath)
    {
        if (saveFileStore.Read(savePath).TryPickProblems(out var problems, out var saveFile))
        {
            return problems.Prepend("Failed to read save file '{0}'", savePath.Path);
        }

        var environmentalObjects = new List<EnvironmentalObject>(saveFile.Environment.Objects.Count);
        foreach (var savedObject in saveFile.Environment.Objects)
        {
            if (savedObject.ToEnvironmentalObject().TryPickProblems(out problems, out var environmentalObject))
            {
                return problems;
            }

            environmentalObjects.Add(environmentalObject);
        }

        return new GameSceneArguments(
            StartingMoney: saveFile.Money,
            Heightmap: saveFile.Terrain.ToHeightmapData(),
            DayStart: new DayTime(saveFile.Time.DayStartHours),
            DayDuration: TimeSpan.FromSeconds(saveFile.Time.DayDurationSeconds),
            CameraOrthographicSize: saveFile.Camera.OrthographicSize,
            EnvironmentalObjects: environmentalObjects,
            TotalGameHours: saveFile.Time.CurrentGameHours);
    }
}
