using System.Globalization;
using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Time;

namespace Olve.Trains.Commands.GameLogic;

public class QueryTimeHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    DayTimeManager dayTimeManager,
    DeltaTimeService deltaTimeService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "query-time";
    public override string HelpString => "Queries the current in-game time and time scale";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var currentTime = dayTimeManager.CurrentTime;

        var json = JsonSerializer.Serialize(new
        {
            value = currentTime.Value.ToString("F4", CultureInfo.InvariantCulture),
            hours = currentTime.Hours,
            minutes = currentTime.Minutes,
            timeScale = deltaTimeService.TimeScale.ToString("F4", CultureInfo.InvariantCulture),
        });

        return new CommandOutput(json);
    }
}
