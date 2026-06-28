using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Time;

public class DayTimeSteppingService(DayTimeManager dayTimeManager, DeltaTimeService deltaTimeService) : ISceneService
{
    internal DayTime DayStartOverride { get; set; } = new(5, 30);
    internal TimeSpan DayDurationOverride { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Total elapsed game-hours to restore on load. When set, it takes precedence over
    /// <see cref="DayStartOverride"/> so a loaded clock keeps both its day and time of day. Null for new games.
    /// </summary>
    internal double? TotalGameHoursOverride { get; set; }

    /// <summary>The time of day a new game's clock starts at.</summary>
    public DayTime DayStart => DayStartOverride;

    public Result Load()
    {
        dayTimeManager.DayLength = DayDurationOverride;

        // A loaded game restores its full elapsed clock; a new game starts at the configured day-start.
        if (TotalGameHoursOverride is { } totalGameHours)
        {
            dayTimeManager.TotalGameHours = totalGameHours;
        }
        else
        {
            dayTimeManager.CurrentTime = DayStartOverride;
        }

        return Result.Success();
    }

    public Result Update()
    {
        dayTimeManager.Step(deltaTimeService.ScaledDeltaTime);

        return Result.Success();
    }
}
