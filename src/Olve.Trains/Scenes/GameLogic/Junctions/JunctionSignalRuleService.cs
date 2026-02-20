using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Utilities.Assertions;
using Olve.Utilities.Types;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

public sealed class JunctionSignalRuleService(ILogger<JunctionSignalRuleService> logger, JunctionSignalService junctionSignalService) : ISceneService
{
    private readonly ConcurrentDictionary<Id<Junction>, List<JunctionSignalRule>> _rules = new();
    private readonly ConcurrentDictionary<Id<JunctionSignalRule>, Id<Junction>> _ruleJunctions = new();
    private static readonly Any Any = new();
    private static JunctionSignalRule GetDefaultRule(Id<Junction> junctionId) => new(Id.New<JunctionSignalRule>(), junctionId, [Any], [Any], [Any], new RoundRobin());
    private static List<JunctionSignalRule> GetDefaultRules(Id<Junction> junctionId) => [ GetDefaultRule(junctionId) ];

    public Result Load()
    {
        junctionSignalService.OnJunctionAdded.Subscribe(OnAdded);
        junctionSignalService.OnJunctionRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
    {
        junctionSignalService.OnJunctionAdded.Unsubscribe(OnAdded);
        junctionSignalService.OnJunctionRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

    private void OnAdded(Id<Junction> junctionId)
    {
        if (!_rules.TryAdd(junctionId, GetDefaultRules(junctionId)))
        {
            logger.LogError("Failed to initialize rules for signal with junction id '{JunctionId}'", junctionId);
        }

        logger.LogDebug("Initialized junction signal rules for junction with id '{JunctionId}'", junctionId);
    }

    private void OnRemoved(Id<Junction> junctionId)
    {
        if (!_rules.Remove(junctionId, out _))
        {
            logger.LogError("Failed to remove rules for signal with junction id '{JunctionId}'", junctionId);
        }

        logger.LogDebug("Removed junction signal rules for junction with id '{JunctionId}'", junctionId);
    }

    public IReadOnlyList<JunctionSignalRule> GetRulesForJunction(Id<Junction> junctionId) => _rules.GetValueOrDefault(junctionId, []);

    public Result<Id<JunctionSignalRule>> AddRuleForJunction(Id<Junction> junctionId,
        IReadOnlyList<SignalRuleVehicle> vehicles,
        IReadOnlyList<SignalRuleSource> sources,
        IReadOnlyList<SignalRuleDestination> destinations,
        SignalRuleDistribution distribution)
    {
        if (!_rules.TryGetValue(junctionId, out var junctionRules))
        {
            return new ResultProblem("Did not find list of signal rules for junction with id '{0}'", junctionId);
        }

        var junctionSignalId = Id.New<JunctionSignalRule>();
        JunctionSignalRule rule = new(junctionSignalId, junctionId, vehicles, sources, destinations, distribution);
        junctionRules.Add(rule);

        _ruleJunctions[junctionSignalId] = junctionId;

        logger.LogDebug("Added signal rule to junction with id '{JunctionId}' (rule count = '{RuleCount}'): {Rule}", junctionId, junctionRules.Count, rule);

        return junctionSignalId;
    }

    public Result RemoveRuleForJunction(Id<JunctionSignalRule> junctionSignalRuleId)
    {
        if (!_ruleJunctions.Remove(junctionSignalRuleId, out var junctionId))
        {
            return new ResultProblem("Did not find junction for signal rule with id '{0}'", junctionSignalRuleId);
        }

        if (!_rules.TryGetValue(junctionId, out var rulesForJunction))
        {
            return new ResultProblem("Did not find junction signal rules for junction with id '{0}'", junctionId);
        }

        var removedRules = rulesForJunction.RemoveAll(x => x.Id == junctionSignalRuleId);
        Assert.That(() => removedRules == 1, "One rule should always be removed with one rule id");

        return Result.Success();
    }

    public Result SetRuleIndex(Id<JunctionSignalRule> junctionSignalRuleId, Index ruleIndex)
    {
        if (Result.Chain(() => _ruleJunctions.GetWithResult(junctionSignalRuleId),
                    _rules.GetWithResult).TryPickProblems(out var problems, out var junctionSignalRules)
            || GetRuleIndex(ruleIndex, junctionSignalRules).TryPickProblems(out problems, out var targetIndex))
        {
            return problems;
        }

        var rule = junctionSignalRules.Single(x => x.Id == junctionSignalRuleId);
        junctionSignalRules.Remove(rule);
        junctionSignalRules.Insert(targetIndex, rule);

        return Result.Success();
    }

    private static Result<int> GetRuleIndex(Index index, List<JunctionSignalRule> junctionSignalRules)
    {
        var targetIndex = index.GetOffset(junctionSignalRules.Count);
        if (targetIndex < 0)
        {
            return new ResultProblem("Got negative rule index '{0}'", targetIndex);
        }

        if (targetIndex >= junctionSignalRules.Count)
        {
            return new ResultProblem("Got rule index exceeding rules list '{0}'", targetIndex);
        }

        return targetIndex;
    }

    public Result ClearRulesForJunction(Id<Junction> junctionId)
    {
        if (!_rules.TryGetValue(junctionId, out var junctionRules))
        {
            return new ResultProblem("Did not find list of signal rules for junction with id '{0}'", junctionId);
        }

        junctionRules.Clear();
        return Result.Success();
    }
}
