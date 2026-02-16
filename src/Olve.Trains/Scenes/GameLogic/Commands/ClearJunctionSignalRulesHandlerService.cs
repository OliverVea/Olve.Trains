using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Junctions;

namespace Olve.Trains.Scenes.GameLogic.Commands;

public class ClearJunctionSignalRulesHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionSignalRuleService junctionSignalRuleService)
    : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument JunctionArgument = new("junction", "The junction to add the rule to", true);
    public override string Verb => "clear-signal-rules";
    public override string HelpString => "Clears all signal rules for a junction";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [JunctionArgument];
    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Junction>(JunctionArgument).TryPickProblems(out var problems, out var junctionId))
        {
            return problems;
        }

        if (junctionSignalRuleService.ClearRulesForJunction(junctionId).TryPickProblems(out problems))
        {
            return problems;
        }

        return CommandOutput.Empty;
    }
}
