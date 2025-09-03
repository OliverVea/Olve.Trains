using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Logging;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Vehicles;

namespace Olve.Trains.Scenes.Game.Junctions;

public class ClearJunctionSignalRules(
    ILoggingManager loggingManager,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionSignalRuleService junctionSignalRuleService)
    : CommandHandlerService(loggingManager, commandHandlerServiceCollection)
{
    private static readonly CommandArgument JunctionArgument = new("junction", "The junction to add the rule to", true);
    public override string Verb => "clear-signal-rules";
    public override string HelpString => "Clears all signal rules for a junction";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [JunctionArgument];
    public override Result Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Junction>(JunctionArgument).TryPickProblems(out var problems, out var junctionId))
        {
            return problems;
        }

        return junctionSignalRuleService.ClearRulesForJunction(junctionId);
    }
}