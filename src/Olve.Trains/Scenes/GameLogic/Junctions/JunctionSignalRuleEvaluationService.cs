using Microsoft.Extensions.Logging;
using Olve.Engine3D;
using Olve.Trains.Scenes.GameLogic.Tracks;
using Olve.Trains.Scenes.GameLogic.Vehicles;

namespace Olve.Trains.Scenes.GameLogic.Junctions;


public class JunctionSignalRuleEvaluationService(ILogger<JunctionSignalRuleEvaluationService> logger,
    VehicleJunctionService vehicleJunctionService,
    JunctionService junctionService,
    JunctionSignalRuleService junctionSignalRuleService)
{
    public Result<RuleEvaluationResult> EvaluateSignalRules(Id<Junction> junctionId, Id<Vehicle> vehicleId, Id<Track> sourceTrackId)
    {
        var junctionSignalRules = junctionSignalRuleService.GetRulesForJunction(junctionId);
        foreach (var junctionSignalRule in junctionSignalRules)
        {
            if (IsEligibleForRule(junctionId, vehicleId, sourceTrackId, junctionSignalRule)
                .TryPickProblems(out var problems, out var isEligibleForRule))
            {
                return problems;
            }
            
            if (!isEligibleForRule)
            {
                continue;
            }
            
            var ruleResult = ApplyRule(vehicleId, sourceTrackId, junctionSignalRule);
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

    private Result<bool> IsEligibleForRule(Id<Junction> junctionId, Id<Vehicle> vehicleId, Id<Track> sourceTrackId, JunctionSignalRule junctionSignalRule)
    {
        if (Result.Concat(
                IsVehicleEligible(vehicleId, junctionSignalRule.Vehicles),
                IsSourceEligible(junctionId, sourceTrackId, junctionSignalRule.Sources)).TryPickProblems(out var problems, out var eligibilities))
        {
            return problems;
        }

        var (isVehicleEligible, isSourceEligible) = eligibilities;
        return isVehicleEligible && isSourceEligible;
    }

    private Result<bool> IsVehicleEligible(Id<Vehicle> sourceVehicleId, IReadOnlyCollection<SignalRuleVehicle> vehicleRules)
    {
        var results = vehicleRules.Select(vehicleRule => IsVehicleEligible(sourceVehicleId, vehicleRule));
        if (results.TryPickProblems(out var problems, out var eligibilities))
        {
            return problems;
        }

        return eligibilities.Any(x => x);
    }

    private Result<bool> IsVehicleEligible(Id<Vehicle> sourceVehicleId, SignalRuleVehicle vehicleRule)
    {
        return vehicleRule.Match<Result<bool>>(
            any => true,
            vehicleGroup =>
            {
                logger.LogWarning("JunctionSignalRuleEvaluationService VehicleGroup is not implemented. Returning false.");
                return false;
            },
            vehicleId => sourceVehicleId == vehicleId);
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

    private Result<RuleEvaluationResult> ApplyRule(Id<Vehicle> vehicleId, Id<Track> sourceTrackId, JunctionSignalRule junctionSignalRule)
    {
        var getAllowedDestinationsResult = GetAllowedDestinations(vehicleId, junctionSignalRule);
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

    private Result<IReadOnlyList<Id<Track>>> GetAllowedDestinations(Id<Vehicle> vehicleId, JunctionSignalRule junctionSignalRule)
    {
        if (vehicleJunctionService.GetVehicleTrackPoint(vehicleId).TryPickProblems(out var problems, out var vehicleTrackPoint))
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
                    allowedDestinations = junctionService.GetConnections(vehicleTrackPoint).Select(x => x.TrackId).ToList();
                    finished = true;
                },
                direction => { },
                trackId =>
                {
                    if (junctionService.IsConnected(trackId, vehicleTrackPoint))
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