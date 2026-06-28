using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Time;

public class DayTimeSteppingService(DayTimeManager dayTimeManager, DeltaTimeService deltaTimeService) : ISceneService
{
    internal DayTime DayStartOverride { get; set; } = new(5, 30);
    internal TimeSpan DayDurationOverride { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>The time of day a new game's clock starts at.</summary>
    public DayTime DayStart => DayStartOverride;

    public Result Load()
    {
        dayTimeManager.CurrentTime = DayStartOverride;
        dayTimeManager.DayLength = DayDurationOverride;

        return Result.Success();
    }

    public Result Update()
    {
        dayTimeManager.Step(deltaTimeService.ScaledDeltaTime);

        return Result.Success();
    }
}
