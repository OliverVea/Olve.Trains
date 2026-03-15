using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Time;

public class DayTimeSteppingService(DayTimeManager dayTimeManager, DeltaTimeService deltaTimeService) : ISceneService
{
    private static readonly DayTime DayStart = new(5, 30);
    private static readonly TimeSpan DayDuration = TimeSpan.FromMinutes(6);

    public Result Load()
    {
        dayTimeManager.CurrentTime = DayStart;
        dayTimeManager.DayLength = DayDuration;

        return Result.Success();
    }

    public Result Update()
    {
        dayTimeManager.Step(deltaTimeService.ScaledDeltaTime);

        return Result.Success();
    }
}