using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Time;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class SetTimeHandlerService(DayTimeManager dayTimeManager, CommandHandlerServiceCollection commandHandlerServiceCollection) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument TimeArgument = new("time", "The time as HH:MM", true);

    public override string Verb => "set-time";
    public override string HelpString => "Sets the current in-game time";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [TimeArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(TimeArgument).Bind(s => s.ParseDayTime())
            .TryPickProblems(out var problems, out var dayTime))
        {
            return problems;
        }

        dayTimeManager.CurrentTime = dayTime;

        return CommandOutput.Empty;
    }
}
