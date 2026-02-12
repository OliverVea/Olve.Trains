using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Time;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleJunctionCrossingService(
    EventQueueFactory eventQueueFactory,
    DayTimeManager dayTimeManager,
    VehiclePositionService vehiclePositionService,
    VehicleJunctionService vehicleJunctionService,
    VehicleMovementService vehicleMovementService,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    JunctionSignalRuleEvaluationService junctionSignalRuleEvaluationService) : ISceneService
{
    private static readonly DayTimeSpan Delay = new(minutes: 5);

    private readonly EventQueue<Id<Vehicle>> _vehicleReachedEndQueue = eventQueueFactory.Create(vehicleMovementService.OnVehicleReachedTrackEnd);
    private readonly PriorityQueue<Id<Vehicle>, long> _queue = new();

    public Result Load()
    {
        _vehicleReachedEndQueue.SetHandler(OnVehicleReachedEnd).Init();
        return Result.Success();
    }

    public Result Unload()
    {
        _vehicleReachedEndQueue.Cleanup();
        return Result.Success();
    }

    public Result OnVehicleReachedEnd(Id<Vehicle> vehicleId)
    {
        if (vehicleJunctionService.GetVehicleJunction(vehicleId).TryPickProblems(out var problems, out var vehicleJunction))
        {
            return problems;
        }

        _queue.Enqueue(vehicleId, dayTimeManager.AbsoluteMinutes);
        return Result.Success();
    }

    public Result Update(TimeSpan deltaTime)
    {
        if (_vehicleReachedEndQueue.Update().TryPickProblems(out var problems))
        {
            return problems;
        }

        while (_queue.TryPeek(out var vehicleId, out var time) && time <= dayTimeManager.AbsoluteMinutes)
        {

            if (CheckVehicle(vehicleId).TryPickProblems(out problems))
            {
                return problems;
            }

            _queue.Dequeue();
        }

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
        var nextEvaluationTime = dayTimeManager.InMinutesFromNow(Delay);
        _queue.Enqueue(vehicleId, nextEvaluationTime);
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