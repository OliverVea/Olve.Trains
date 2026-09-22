using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Time;

public class DayTimeSteppingService(
    DayTimeManager dayTimeManager,
    DeltaTimeService deltaTimeService,
    GameSceneArguments arguments) : ISceneService
{
    /// <summary>The time of day a new game's clock starts at.</summary>
    public DayTime DayStart => arguments.DayStart ?? GameSceneArguments.DefaultDayStart;

    public Result Load()
    {
        dayTimeManager.DayLength = arguments.DayDuration ?? GameSceneArguments.DefaultDayDuration;

        if (arguments.TotalGameHours is { } totalGameHours)
        {
            dayTimeManager.TotalGameHours = totalGameHours;
        }
        else
        {
            dayTimeManager.CurrentTime = DayStart;
        }

        return Result.Success();
    }

    public Result Update()
    { 
        return dayTimeManager.Step(deltaTimeService.ScaledDeltaTime);
    }
}
