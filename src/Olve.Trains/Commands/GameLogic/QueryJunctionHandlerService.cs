using System.Text.Json;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Junctions;

namespace Olve.Trains.Commands.GameLogic;

public class QueryJunctionHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    JunctionSignalRuleService junctionSignalRuleService) : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument JunctionArgument = new("junction", "The junction ID to query", true);

    public override string Verb => "query-junction";
    public override string HelpString => "Queries junction details including connections and signal rules";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [JunctionArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Junction>(JunctionArgument).TryPickProblems(out var problems, out var junctionId))
        {
            return problems;
        }

        if (!junctionService.TryGetJunction(junctionId, out var junction))
        {
            return new ResultProblem("Junction '{0}' not found", junctionId);
        }

        var connections = junctionService.GetConnections(junctionId)
            .Select(c => new
            {
                trackId = c.TrackId.ToString(),
                direction = c.TrackEndpoint.Tangent.ToCardinalDirection().ToString().ToLowerInvariant(),
            })
            .ToArray();

        var hasSignal = junctionSignalService.JunctionHasSignal(junctionId)
            .TryPickProblems(out _, out var signal) ? false : signal;

        var rules = junctionSignalRuleService.GetRulesForJunction(junctionId)
            .Select(SerializeRule)
            .ToArray();

        var json = JsonSerializer.Serialize(new
        {
            junctionId = junctionId.ToString(),
            position = new { x = junction.Position.X, z = junction.Position.Z },
            hasSignal,
            connections,
            rules,
        });

        return new CommandOutput(json);
    }

    private static object SerializeRule(JunctionSignalRule rule)
    {
        return new
        {
            ruleId = rule.Id.ToString(),
            vehicles = rule.Vehicles.Select(SerializeVehicleConstraint).ToArray(),
            sources = rule.Sources.Select(SerializeSourceConstraint).ToArray(),
            destinations = rule.Destinations.Select(SerializeDestinationConstraint).ToArray(),
            distribution = rule.Distribution.Match(
                _ => "RoundRobin"),
        };
    }

    private static string SerializeVehicleConstraint(SignalRuleVehicle v)
    {
        return v.Match(
            _ => "any",
            groupId => $"group({groupId})",
            vehicleId => $"vehicle({vehicleId})");
    }

    private static string SerializeSourceConstraint(SignalRuleSource s)
    {
        return s.Match(
            _ => "any",
            dir => $"direction({dir.ToString().ToLowerInvariant()})",
            trackId => $"track({trackId})");
    }

    private static string SerializeDestinationConstraint(SignalRuleDestination d)
    {
        return d.Match(
            _ => "any",
            dir => $"direction({dir.ToString().ToLowerInvariant()})",
            trackId => $"track({trackId})");
    }
}
