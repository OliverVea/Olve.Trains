using System.Globalization;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Time;

namespace Olve.Trains.Commands.GameLogic;

public class SetSpeedHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    DeltaTimeService deltaTimeService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument ScaleArgument = new("scale", "The time scale multiplier (0 = paused)", true);

    public override string Verb => "set-speed";
    public override string HelpString => "Sets the game time scale";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [ScaleArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetRequiredArgument(ScaleArgument)
            .Bind(s => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                ? Result<float>.Success(v)
                : new ResultProblem("Could not parse '{0}' as a float", s))
            .TryPickProblems(out var problems, out var scale))
        {
            return problems;
        }

        deltaTimeService.TimeScale = scale;

        return CommandOutput.Empty;
    }
}
