using Olve.Engine3D.Scenes;
using Olve.Engine3D.Time;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Time;

public class DayTimeSteppingService(ILoggingManager loggingManager, DayTimeManager dayTimeManager) : SceneService(loggingManager)
{
    private static readonly DayTime DayStart = new(5, 30);
    private static readonly TimeSpan DayDuration = TimeSpan.FromMinutes(6);
    
    protected override Result OnLoad()
    {
        dayTimeManager.CurrentTime = DayStart;
        dayTimeManager.DayLength = DayDuration;

        return Result.Success();
    }

    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        dayTimeManager.Step(deltaTime);

        return Result.Success();
    }
}