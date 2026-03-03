using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleJunctionCrossingService(
    VehiclePositionService vehiclePositionService,
    VehicleJunctionService vehicleJunctionService,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    JunctionSignalRuleEvaluationService junctionSignalRuleEvaluationService) : ISceneService
{
    private readonly List<Id<Vehicle>> _queue = new();

    public Result OnVehicleReachedEnd(Id<Vehicle> vehicleId)
    {
        _queue.Add(vehicleId);
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        foreach (var vehicleId in _queue)
        {

            if (CheckVehicle(vehicleId).TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        _queue.Clear();

        return Result.Success();
    }

    private Result CheckVehicle(Id<Vehicle> vehicleId)
    {
        var getVehicleJunctionResult = vehicleJunctionService.GetVehicleJunction(vehicleId);
        if (getVehicleJunctionResult.TryPickProblems(out var problems, out var vehicleJunction))
        {
            return problems;
        }

        if (!vehicleJunction.TryPickT1(out var junctionId, out _))
        {
            return Result.Success();
        }

        if (!vehiclePositionService.TryGetTrackPosition(vehicleId, out var trackPosition))
        {
            return new ResultProblem("Vehicle does not have a track position - likely not on a track");
        }

        if (junctionSignalService.JunctionHasSignal(junctionId).TryPickProblems(out problems, out var junctionHasSignal))
        {
            return problems;
        }

        if (!junctionHasSignal)
        {
            var connections = junctionService.GetConnections(junctionId);
            var connection = connections.FirstOrDefault(x => x.TrackId != trackPosition.TrackId);
            if (connection == default)
            {
                return SuspendVehicle(vehicleId);
            }

            TransferredTracks transferredTracks = new(trackPosition.TrackId, connection.TrackId);
            return TransferTracks(vehicleId, trackPosition, transferredTracks);
        }

        var evaluateSignalRulesResult = junctionSignalRuleEvaluationService.EvaluateSignalRules(junctionId, vehicleId, trackPosition.TrackId);
        if (evaluateSignalRulesResult.TryPickProblems(out problems, out var ruleEvaluation))
        {
            return problems;
        }

        return ruleEvaluation.Match(
            none => SuspendVehicle(vehicleId),
            transferredTracks => TransferTracks(vehicleId, trackPosition, transferredTracks));
    }

    private Result SuspendVehicle(Id<Vehicle> vehicleId)
    {
        _queue.Add(vehicleId);
        return Result.Success();
    }

    private Result TransferTracks(Id<Vehicle> vehicleId, VehicleTrackPosition vehicleTrackPosition, TransferredTracks transferredTracks)
    {
        var isAtDestinationEndResult = vehicleJunctionService.IsAtTrackEnd(vehicleId, transferredTracks.To);
        if (isAtDestinationEndResult.TryPickProblems(out var problems, out var isAtDestinationEnd))
        {
            return problems;
        }

        var newVelocity = isAtDestinationEnd ? -float.Abs(vehicleTrackPosition.Velocity) : float.Abs(vehicleTrackPosition.Velocity);
        var newTime = isAtDestinationEnd ? 1 : 0;

        TrackPoint newVehicleTrackPoint = new(transferredTracks.To, newTime);
        VehicleTrackPosition newVehicleTrackPosition = new(newVehicleTrackPoint, newVelocity);
        return vehiclePositionService.SetTrackPosition(vehicleId, newVehicleTrackPosition);
    }
}
