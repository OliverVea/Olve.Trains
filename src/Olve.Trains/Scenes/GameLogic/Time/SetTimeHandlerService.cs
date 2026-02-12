using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Time;

public class SetTimeHandlerService(DayTimeManager dayTimeManager, CommandHandlerServiceCollection commandHandlerServiceCollection) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TimeArgument = new("time", "The time as HH:MM", true);
    
    public override string Verb => "set-time";
    public override string HelpString => "Sets the current in-game time";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TimeArgument];

    public override Result Handle(CommandContext commandContext)
    {
        var dayTimeString = commandContext.GetArgument(TimeArgument)!;
        if (!TryParseDayTime(dayTimeString, out var dayTime))
        {
            return new ResultProblem("Could not parse day time '{0}'", dayTimeString);
        }

        dayTimeManager.CurrentTime = dayTime;

        return Result.Success();
    }

    private static bool TryParseDayTime(string dayTimeString, out DayTime dayTime)
    {
        var parts = dayTimeString.Split(':');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberFormatInfo.InvariantInfo, out var hour)
            || !int.TryParse(parts[1], NumberFormatInfo.InvariantInfo, out var minute))
        {
            dayTime = default;
            return false;
        }
        
        dayTime = new DayTime(hour, minute);
        return true;
    }
}