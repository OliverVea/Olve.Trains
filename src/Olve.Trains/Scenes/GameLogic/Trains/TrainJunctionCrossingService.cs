using Olve.Engine3D.Scenes;
using Olve.Trains.Scenes.GameLogic.Junctions;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainJunctionCrossingService(
    TrainPositionService trainPositionService,
    TrainJunctionService trainJunctionService,
    JunctionService junctionService,
    JunctionSignalService junctionSignalService,
    JunctionSignalRuleEvaluationService junctionSignalRuleEvaluationService,
    TrainTrackHistoryService trainTrackHistoryService,
    TrainMovementService trainMovementService) : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trainMovementService]);
    private readonly List<Id<Train>> _queue = new();
    private readonly List<Id<Train>> _suspended = new();

    public Result OnTrainReachedEnd(Id<Train> trainId)
    {
        _queue.Add(trainId);
        return Result.Success();
    }

    public Result Update()
    {
        _suspended.Clear();

        foreach (var trainId in _queue)
        {
            if (CheckTrain(trainId).TryPickProblems(out var problems))
            {
                return problems;
            }
        }

        _queue.Clear();
        _queue.AddRange(_suspended);

        return Result.Success();
    }

    private Result CheckTrain(Id<Train> trainId)
    {
        var getTrainJunctionResult = trainJunctionService.GetTrainJunction(trainId);
        if (getTrainJunctionResult.TryPickProblems(out var problems, out var trainJunction))
        {
            return problems;
        }

        if (!trainJunction.TryPickT1(out var junctionId, out _))
        {
            return Result.Success();
        }

        if (!trainPositionService.TryGetTrackPosition(trainId, out var trackPosition))
        {
            return new ResultProblem("Train does not have a track position - likely not on a track");
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
                return SuspendTrain(trainId);
            }

            TransferredTracks transferredTracks = new(trackPosition.TrackId, connection.TrackId);
            return TransferTracks(trainId, trackPosition, transferredTracks);
        }

        var evaluateSignalRulesResult = junctionSignalRuleEvaluationService.EvaluateSignalRules(junctionId, trainId, trackPosition.TrackId);
        if (evaluateSignalRulesResult.TryPickProblems(out problems, out var ruleEvaluation))
        {
            return problems;
        }

        return ruleEvaluation.Match(
            none => SuspendTrain(trainId),
            transferredTracks => TransferTracks(trainId, trackPosition, transferredTracks));
    }

    private Result SuspendTrain(Id<Train> trainId)
    {
        _suspended.Add(trainId);
        return Result.Success();
    }

    private Result TransferTracks(Id<Train> trainId, TrainTrackPosition trainTrackPosition, TransferredTracks transferredTracks)
    {
        var isAtDestinationEndResult = trainJunctionService.IsAtTrackEnd(trainId, transferredTracks.To);
        if (isAtDestinationEndResult.TryPickProblems(out var problems, out var isAtDestinationEnd))
        {
            return problems;
        }

        var newVelocity = isAtDestinationEnd ? -float.Abs(trainTrackPosition.Velocity) : float.Abs(trainTrackPosition.Velocity);
        var newTargetVelocity = isAtDestinationEnd ? -float.Abs(trainTrackPosition.TargetVelocity) : float.Abs(trainTrackPosition.TargetVelocity);
        var newTime = isAtDestinationEnd ? 1 : 0;

        trainTrackHistoryService.RecordTransition(trainId, trainTrackPosition.TrackId, trainTrackPosition.Velocity);

        TrackPoint newTrainTrackPoint = new(transferredTracks.To, newTime);
        TrainTrackPosition newTrainTrackPosition = new(newTrainTrackPoint, newVelocity, newTargetVelocity, trainTrackPosition.Acceleration);
        return trainPositionService.SetTrackPosition(trainId, newTrainTrackPosition);
    }
}
