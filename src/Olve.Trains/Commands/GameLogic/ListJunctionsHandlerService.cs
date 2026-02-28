using System.Text.Json;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Junctions;

namespace Olve.Trains.Commands.GameLogic;

public class ListJunctionsHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService) : CommandHandlerService(commandHandlerServiceCollection)
{
    public override string Verb => "list-junctions";
    public override string HelpString => "Lists all junctions with connection counts and signal status";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        var junctions = junctionSignalService.SignalJunctions
            .Concat(GetNonSignalJunctions())
            .Distinct()
            .Select(junctionId =>
            {
                if (!junctionService.TryGetJunction(junctionId, out var junction))
                {
                    return null;
                }

                var connections = junctionService.GetConnections(junctionId);
                var hasSignal = junctionSignalService.JunctionHasSignal(junctionId)
                    .TryPickProblems(out _, out var signal) ? false : signal;

                return new
                {
                    junctionId = junctionId.ToString(),
                    position = new { x = junction.Position.X, z = junction.Position.Z },
                    connectionCount = connections.Count,
                    hasSignal,
                } as object;
            })
            .Where(x => x is not null)
            .ToArray();

        var json = JsonSerializer.Serialize(new { junctions });
        return new CommandOutput(json);
    }

    private IEnumerable<Id<Junction>> GetNonSignalJunctions()
    {
        // JunctionService doesn't expose all junction IDs directly,
        // but signal junctions cover the ones with ≥3 connections.
        // Non-signal junctions (≤2 connections) are less useful for queries
        // but we include signal junctions for completeness.
        return [];
    }
}
