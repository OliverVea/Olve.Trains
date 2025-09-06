using System.Text.RegularExpressions;
using Olve.Engine3D;
using Olve.Engine3D.DebugServer.Commands;
using Olve.Engine3D.Logging;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Trains.Scenes.Game.Vehicles;
using Olve.Utilities.Types;
using RuleFields = (System.Collections.Generic.List<Olve.Trains.Scenes.Game.Junctions.SignalRuleVehicle> Vehicles, 
    System.Collections.Generic.List<Olve.Trains.Scenes.Game.Junctions.SignalRuleSource> Sources, 
    System.Collections.Generic.List<Olve.Trains.Scenes.Game.Junctions.SignalRuleDestination> Destinations, 
    Olve.Trains.Scenes.Game.Junctions.SignalRuleDistribution Distribution);

namespace Olve.Trains.Scenes.Game.Junctions;

public class AddJunctionRuleHandlerService(
    ILoggingManager loggingManager,
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionSignalRuleService junctionSignalRuleService)
    : CommandHandlerService(loggingManager, commandHandlerServiceCollection)
{
    private static readonly CommandArgument JunctionArgument = new("junction", "The junction to add the rule to", true);
    private static readonly CommandArgument RuleArgument = new("rule", "The rule to add", true);

    public override string Verb => "add-signal-rule";
    public override string HelpString => "Adds a signal rule to the specified junction signal";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [JunctionArgument, RuleArgument];

    public override Result Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Junction>(JunctionArgument).TryPickProblems(out var problems, out var junctionId)
            || ParseRule(commandContext.Arguments[RuleArgument.Key]).TryPickProblems(out problems, out var ruleFields))
        {
            return problems;
        }

        return junctionSignalRuleService.AddRuleForJunction(junctionId,
            ruleFields.Vehicles,
            ruleFields.Sources,
            ruleFields.Destinations,
            ruleFields.Distribution).ToEmptyResult();
    }

    private const string RulePattern = @"^\s*
         (?<vehicle>.+?)\s+from\b\s+
         (?<source>.+?)\s+to\b\s+
         (?<destination>.+?)\s+with\b\s+
         (?<distribution>.+?)\s*$
        ";

    private static Result<(string Vehicles, string Sources, string Destinations, string Distribution)>
        ParseRuleString(string rule)
    {
#pragma warning disable MA0009
        var match = Regex.Match(rule, RulePattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace);
#pragma warning restore MA0009

        if (!match.Success)
        {
            return new ResultProblem("Failed to parse rule '{0}'", rule);
        }

        var vehicles = match.Groups["vehicle"].Value;
        var sources = match.Groups["source"].Value;
        var destinations = match.Groups["destination"].Value;
        var distribution = match.Groups["distribution"].Value;

        return (vehicles, sources, destinations, distribution);
    }

    private static Result<RuleFields> ParseRule(string rule)
    {
        if (ParseRuleString(rule).TryPickProblems(out var problems, out var ruleFields))
        {
            return problems.Prepend("Failed to parse rule fields");
        }
        
        var vehiclesResult = ParseVehicles(ruleFields.Vehicles.Trim());
        var sourcesResult = ParseSources(ruleFields.Sources.Trim());
        var destinationsResult = ParseDestinations(ruleFields.Destinations.Trim());
        var distributionResult = ParseDistribution(ruleFields.Distribution.Trim());
        
        return Result.Concat<List<SignalRuleVehicle>, List<SignalRuleSource>, List<SignalRuleDestination>, SignalRuleDistribution>(
            vehiclesResult, sourcesResult, destinationsResult, distributionResult);
    }

    private static Result<List<SignalRuleVehicle>> ParseVehicles(string vehicleStrings)
    {
        if (vehicleStrings.Length == 0)
        {
            return new ResultProblem("Got empty vehicles");
        }

        return ParseField(vehicleStrings, ParseVehicle);
    }

    private static Result<SignalRuleVehicle> ParseVehicle(string vehicle)
    {
        if (string.Equals(vehicle, "any", StringComparison.InvariantCultureIgnoreCase)) return Result.Success<SignalRuleVehicle>(new Any());
        if (vehicle.StartsWith("vehicle", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Vehicle>(vehicle).MapValue(x => (SignalRuleVehicle)x);
        if (vehicle.StartsWith("group", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<VehicleGroup>(vehicle).MapValue(x => (SignalRuleVehicle)x);
        return new ResultProblem("Could not parse vehicle '{0}'", vehicle);
    }

    private static Result<List<SignalRuleSource>> ParseSources(string sources)
    {
        if (sources.Length == 0)
        {
            return new ResultProblem("Got empty sources");
        }

        return ParseField(sources, ParseSource);
    }

    private static Result<SignalRuleSource> ParseSource(string source)
    {
        if (string.Equals(source, "any", StringComparison.InvariantCultureIgnoreCase)) return Result.Success<SignalRuleSource>(new Any());
        if (source.StartsWith("track", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Track>(source).MapValue(x => (SignalRuleSource)x);
        if (source.StartsWith("direction", StringComparison.InvariantCultureIgnoreCase)) return ParseEnumArgument<CardinalDirection>(source).MapValue(x => (SignalRuleSource)x);
        return new ResultProblem("Could not parse source '{0}'", source);
    }

    private static Result<List<SignalRuleDestination>> ParseDestinations(string destinations)
    {
        if (destinations.Length == 0)
        {
            return new ResultProblem("Got empty destinations");
        }

        return ParseField(destinations, ParseDestination);
    }

    private static Result<SignalRuleDestination> ParseDestination(string destination)
    {
        if (string.Equals(destination, "any", StringComparison.InvariantCultureIgnoreCase)) return Result.Success<SignalRuleDestination>(new Any());
        if (destination.StartsWith("track", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Track>(destination).MapValue(x => (SignalRuleDestination)x);
        if (destination.StartsWith("direction", StringComparison.InvariantCultureIgnoreCase)) return ParseEnumArgument<CardinalDirection>(destination).MapValue(x => (SignalRuleDestination)x);
        return new ResultProblem("Could not parse destination '{0}'", destination);
    }

    private static SignalRuleDistribution ParseDistribution(string distribution)
    {
        return new RoundRobin();
    }

    private static bool IsList(string s) => s.StartsWith('[');
    private static IEnumerable<string> ParseList(string s) => s[1..^1].Split(',').Select(x => x.Trim());

    private static Result<List<T>> ParseField<T>(string s, Func<string, Result<T>> parseSingle)
    {
        return !IsList(s)
            ? parseSingle(s).TryPickProblems(out var problems, out var value) 
                ? problems
                : new List<T> { value }
            : ParseList(s).Select(parseSingle).TryPickProblems(out problems, out var values)
                ? problems
                : values.ToList();
    }

    private static Result<Id<T>> ParseIdArgument<T>(string stringWithArgument)
    {
        return Result.Chain(() => GetArgument(stringWithArgument), Id<T>.Parse);
    }

    private static Result<T> ParseEnumArgument<T>(string stringWithArgument)
    {
        return Result.Chain(() => GetArgument(stringWithArgument), ParseEnum<T>);
    }

    private static Result<T> ParseEnum<T>(string enumString)
    {
        var result = Enum.Parse(typeof(T), enumString, true);
        if (result is T parsedValue)
        {
            return parsedValue;
        }

        return new ResultProblem("Could not convert value '{0}' into '{1}'", enumString, nameof(T));
    }
    
    private static Result<string> GetArgument(string stringWithArgument)
    {
        var start = stringWithArgument.IndexOf('(');
        var end = stringWithArgument.LastIndexOf(')');

        if (start == -1) return new ResultProblem("Found no starting parenthesis '('");
        if (end == -1) return new ResultProblem("Found no ending parenthesis '('");

        return stringWithArgument[(start+1)..end];
    }

}