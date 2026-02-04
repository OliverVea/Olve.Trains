using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Time;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Junctions;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleJunctionCrossingService(ILoggingManager loggingManager,
    DayTimeManager dayTimeManager,
    VehiclePositionService vehiclePositionService,
    VehicleJunctionService vehicleJunctionService,
    VehicleMovementService vehicleMovementService,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    JunctionSignalRuleEvaluationService junctionSignalRuleEvaluationService) : SceneService(loggingManager)
{
    private static readonly DayTimeSpan Delay = new(minutes: 5);
    
    private readonly EventQueue<Id<Vehicle>> _vehicleReachedEndQueue = new(vehicleMovementService.OnVehicleReachedTrackEnd);
    private readonly PriorityQueue<Id<Vehicle>, long> _queue = new();

    protected override Result OnLoad()
    {
        _vehicleReachedEndQueue.SetHandler(OnVehicleReachedEnd).Init();
        return Result.Success();
    }

    protected override Result OnUnload()
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

    protected override Result OnUpdate(TimeSpan deltaTime)
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

        VehicleTrackPosition newVehicleTrackPosition = new(transferredTracks.To, newTime, newVelocity);
        return vehiclePositionService.SetTrackPosition(vehicleId, newVehicleTrackPosition);
    }
}