using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.Game.Time;

public class DayTimeSteppingService(DayTimeManager dayTimeManager) : ISceneService
{
    private static readonly DayTime DayStart = new(5, 30);
    private static readonly TimeSpan DayDuration = TimeSpan.FromMinutes(6);
    
    public Result Load()
    {
        dayTimeManager.CurrentTime = DayStart;
        dayTimeManager.DayLength = DayDuration;

        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        dayTimeManager.Step(deltaTime);

        return Result.Success();
    }
}