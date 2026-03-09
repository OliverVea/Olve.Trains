using System.Text.RegularExpressions;
using Olve.Engine3D;
using Olve.Engine3D.Commands;
using Olve.Engine3D.Logging;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;
using Olve.Utilities.Types;
using RuleFields = (System.Collections.Generic.List<Olve.Trains.Scenes.GameLogic.Junctions.SignalRuleTrain> Trains,
    System.Collections.Generic.List<Olve.Trains.Scenes.GameLogic.Junctions.SignalRuleSource> Sources,
    System.Collections.Generic.List<Olve.Trains.Scenes.GameLogic.Junctions.SignalRuleDestination> Destinations,
    Olve.Trains.Scenes.GameLogic.Junctions.SignalRuleDistribution Distribution);

namespace Olve.Trains.Commands.GameLogic;

public class AddJunctionRuleHandlerService(
    CommandHandlerServiceCollection commandHandlerServiceCollection,
    JunctionSignalRuleService junctionSignalRuleService)
    : CommandHandlerService(commandHandlerServiceCollection)
{
    private static readonly CommandArgument JunctionArgument = new("junction", "The junction to add the rule to", true);
    private static readonly CommandArgument RuleArgument = new("rule", "The rule to add", true);

    public override string Verb => "add-signal-rule";
    public override string HelpString => "Adds a signal rule to the specified junction signal";
    public override IReadOnlyList<CommandArgument> Arguments { get; } = [JunctionArgument, RuleArgument];

    public override Result<CommandOutput> Handle(CommandContext commandContext)
    {
        if (commandContext.GetId<Junction>(JunctionArgument).TryPickProblems(out var problems, out var junctionId)
            || ParseRule(commandContext.Arguments[RuleArgument.Key]).TryPickProblems(out problems, out var ruleFields))
        {
            return problems;
        }

        if (junctionSignalRuleService.AddRuleForJunction(junctionId,
                ruleFields.Trains,
                ruleFields.Sources,
                ruleFields.Destinations,
                ruleFields.Distribution).ToEmptyResult().TryPickProblems(out problems))
        {
            return problems;
        }

        return CommandOutput.Empty;
    }

    private const string RulePattern = @"^\s*
         (?<train>.+?)\s+from\b\s+
         (?<source>.+?)\s+to\b\s+
         (?<destination>.+?)\s+with\b\s+
         (?<distribution>.+?)\s*$
        ";

    private static Result<(string Trains, string Sources, string Destinations, string Distribution)>
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

        var trains = match.Groups["train"].Value;
        var sources = match.Groups["source"].Value;
        var destinations = match.Groups["destination"].Value;
        var distribution = match.Groups["distribution"].Value;

        return (trains, sources, destinations, distribution);
    }

    private static Result<RuleFields> ParseRule(string rule)
    {
        if (ParseRuleString(rule).TryPickProblems(out var problems, out var ruleFields))
        {
            return problems.Prepend("Failed to parse rule fields");
        }

        var trainsResult = ParseTrains(ruleFields.Trains.Trim());
        var sourcesResult = ParseSources(ruleFields.Sources.Trim());
        var destinationsResult = ParseDestinations(ruleFields.Destinations.Trim());
        var distributionResult = ParseDistribution(ruleFields.Distribution.Trim());

        return Result.Concat<List<SignalRuleTrain>, List<SignalRuleSource>, List<SignalRuleDestination>, SignalRuleDistribution>(
            trainsResult, sourcesResult, destinationsResult, distributionResult);
    }

    private static Result<List<SignalRuleTrain>> ParseTrains(string trainStrings)
    {
        if (trainStrings.Length == 0)
        {
            return new ResultProblem("Got empty trains");
        }

        return ParseField(trainStrings, ParseTrain);
    }

    private static Result<SignalRuleTrain> ParseTrain(string train)
    {
        if (string.Equals(train, "any", StringComparison.InvariantCultureIgnoreCase)) return Result.Success<SignalRuleTrain>(new Any());
        if (train.StartsWith("train", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Train>(train).Map(x => (SignalRuleTrain)x);
        if (train.StartsWith("group", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<TrainGroup>(train).Map(x => (SignalRuleTrain)x);
        return new ResultProblem("Could not parse train '{0}'", train);
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
        if (source.StartsWith("track", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Track>(source).Map(x => (SignalRuleSource)x);
        if (source.StartsWith("direction", StringComparison.InvariantCultureIgnoreCase)) return ParseEnumArgument<CardinalDirection>(source).Map(x => (SignalRuleSource)x);
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
        if (destination.StartsWith("track", StringComparison.InvariantCultureIgnoreCase)) return ParseIdArgument<Track>(destination).Map(x => (SignalRuleDestination)x);
        if (destination.StartsWith("direction", StringComparison.InvariantCultureIgnoreCase)) return ParseEnumArgument<CardinalDirection>(destination).Map(x => (SignalRuleDestination)x);
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
        return Result.Chain(() => GetArgument(stringWithArgument), s => Id.TryParse<T>(s, out var id) ? Result.Success(id) : new ResultProblem("Could not parse '{0}' to Id<{1}>", s, typeof(T).Name));
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

        return new ResultProblem("Could not convert value '{0}' into '{1}'", enumString, typeof(T).Name);
    }

    private static Result<string> GetArgument(string stringWithArgument)
    {
        var start = stringWithArgument.IndexOf('(');
        var end = stringWithArgument.LastIndexOf(')');

        if (start == -1) return new ResultProblem("Found no starting parenthesis '('");
        if (end == -1) return new ResultProblem("Found no ending parenthesis ')'");

        return stringWithArgument[(start+1)..end];
    }

}
