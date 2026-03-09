using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Trains;

namespace Olve.Trains.Scenes.GameLogic.Junctions;


public class JunctionSignalRuleEvaluationService(
    TrainJunctionService trainJunctionService,
    JunctionService junctionService,
    JunctionSignalRuleService junctionSignalRuleService,
    TrainGroupService trainGroupService)
{
    public Result<RuleEvaluationResult> EvaluateSignalRules(Id<Junction> junctionId, Id<Train> trainId, Id<Track> sourceTrackId)
    {
        var junctionSignalRules = junctionSignalRuleService.GetRulesForJunction(junctionId);
        foreach (var junctionSignalRule in junctionSignalRules)
        {
            if (IsEligibleForRule(junctionId, trainId, sourceTrackId, junctionSignalRule)
                .TryPickProblems(out var problems, out var isEligibleForRule))
            {
                return problems;
            }
            
            if (!isEligibleForRule)
            {
                continue;
            }
            
            var ruleResult = ApplyRule(trainId, sourceTrackId, junctionSignalRule);
            if (ruleResult.TryPickProblems(out problems, out var ruleEvaluation))
            {
                return problems;
            }

            if (ruleEvaluation.TryPickT1(out var transferredTracks, out _))
            {
                return Result.Success<RuleEvaluationResult>(transferredTracks);
            }
        }

        return RuleEvaluationResult.None;
    }

    private Result<bool> IsEligibleForRule(Id<Junction> junctionId, Id<Train> trainId, Id<Track> sourceTrackId, JunctionSignalRule junctionSignalRule)
    {
        if (Result.Concat(
                IsTrainEligible(trainId, junctionSignalRule.Trains),
                IsSourceEligible(junctionId, sourceTrackId, junctionSignalRule.Sources)).TryPickProblems(out var problems, out var eligibilities))
        {
            return problems;
        }

        var (isTrainEligible, isSourceEligible) = eligibilities;
        return isTrainEligible && isSourceEligible;
    }

    private Result<bool> IsTrainEligible(Id<Train> sourceTrainId, IReadOnlyCollection<SignalRuleTrain> trainRules)
    {
        var results = trainRules.Select(trainRule => IsTrainEligible(sourceTrainId, trainRule));
        if (results.TryPickProblems(out var problems, out var eligibilities))
        {
            return problems;
        }

        return eligibilities.Any(x => x);
    }

    private Result<bool> IsTrainEligible(Id<Train> sourceTrainId, SignalRuleTrain trainRule)
    {
        return trainRule.Match<Result<bool>>(
            any => true,
            groupId => trainGroupService.IsMemberOf(sourceTrainId, groupId),
            trainId => sourceTrainId == trainId);
    }

    private Result<bool> IsSourceEligible(Id<Junction> junctionId, Id<Track> sourceTrackId, IReadOnlyCollection<SignalRuleSource> sourceRules)
    {
        var results = sourceRules.Select(sourceRule => IsSourceEligible(junctionId, sourceTrackId, sourceRule));
        if (results.TryPickProblems(out var problems, out var eligibilities))
        {
            return problems;
        }

        return eligibilities.Any(x => x);
    }

    private Result<bool> IsSourceEligible(Id<Junction> junctionId, Id<Track> sourceTrackId, SignalRuleSource sourceRule)
    {
        return sourceRule.Match<Result<bool>>(
            any => true,
            cardinalDirection =>
            {
                var connections = junctionService.GetConnections(junctionId);
                var connection = connections.FirstOrDefault(c => c.TrackId == sourceTrackId);
                if (connection == default)
                {
                    return false;
                }

                var incomingDirection = (-connection.TrackEndpoint.Tangent).ToCardinalDirection();
                return incomingDirection == cardinalDirection;
            },
            trackId => sourceTrackId == trackId);
    }

    private Result<RuleEvaluationResult> ApplyRule(Id<Train> trainId, Id<Track> sourceTrackId, JunctionSignalRule junctionSignalRule)
    {
        var getAllowedDestinationsResult = GetAllowedDestinations(trainId, junctionSignalRule);
        if (getAllowedDestinationsResult.TryPickProblems(out var problems, out var allowedDestinations))
        {
            return problems;
        }

        if (allowedDestinations.Count == 0)
        {
            return RuleEvaluationResult.None;
        }

        var destinationTrackId = junctionSignalRule.Distribution.Match(
            roundRobin => roundRobin.Sample(allowedDestinations));
        
        TransferredTracks transferredTracks = new(sourceTrackId, destinationTrackId);
        return Result<RuleEvaluationResult>.Success(transferredTracks);

    }

    private Result<IReadOnlyList<Id<Track>>> GetAllowedDestinations(Id<Train> trainId, JunctionSignalRule junctionSignalRule)
    {
        if (trainJunctionService.GetTrainTrackPoint(trainId).TryPickProblems(out var problems, out var trainTrackPoint))
        {
            return problems;
        }
        
        List<Id<Track>> allowedDestinations = [];
        var finished = false;

        foreach (var destinationRule in junctionSignalRule.Destinations)
        {
            destinationRule.Switch(
                any =>
                {
                    allowedDestinations = junctionService.GetConnections(trainTrackPoint).Select(x => x.TrackId).ToList();
                    finished = true;
                },
                direction => { },
                trackId =>
                {
                    if (junctionService.IsConnected(trackId, trainTrackPoint))
                    {
                        allowedDestinations.Add(trackId);
                    }
                });

            if (finished)
            {
                break;
            }
        }

        return allowedDestinations;
    }
}