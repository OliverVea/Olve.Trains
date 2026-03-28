using Microsoft.Extensions.Logging;
using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Time;
using Olve.Engine3D.Utilities;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainMovementService(ILogger<TrainMovementService> logger,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService,
    DeltaTimeService deltaTimeService) : ISceneService
{
    public Event<Id<Train>> OnTrainReachedTrackEnd { get; } = new();

    public Result Update()
    {
        var deltaTime = deltaTimeService.ScaledDeltaTime;

        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (!trainPositionService.TryGetMotion(trainId, out var motion))
            {
                continue;
            }

            var result = UpdateTrackPosition(trainId, deltaTime, trackPosition, motion);
            if (result.TryPickProblems(out var problems))
            {
                logger.LogWarning("Failed to update track position: {Problems}", problems);
            }
        }

        return Result.Success();
    }

    private Result UpdateTrackPosition(Id<Train> trainId, TimeSpan deltaTime, TrainTrackPosition trackPosition, TrainMotion motion)
    {
        if (trackSplineService.GetLength(trackPosition.TrackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }

        var dt = deltaTime.InSeconds();
        var speed = motion.Speed;
        var target = motion.TargetSpeed;

        if (speed != target)
        {
            var sign = target > speed ? 1f : -1f;
            speed += sign * motion.Acceleration * dt;
            speed = sign > 0 ? float.Min(speed, target) : float.Max(speed, target);
            trainPositionService.SetMotion(trainId, motion with { Speed = speed });
        }

        var directionSign = trackPosition.Direction == TrainDirection.Forward ? 1f : -1f;
        var newTime = trackPosition.Time + directionSign * speed * dt / trackLength;

        var reachedEndOfTrack = newTime < 0 || newTime > 1;
        newTime = float.Clamp(newTime, 0, 1);

        var newTrackPoint = trackPosition.TrackPoint with { Time = newTime };
        trackPosition = trackPosition with { TrackPoint = newTrackPoint };
        trainPositionService.SetTrackPosition(trainId, trackPosition);

        if (reachedEndOfTrack)
        {
            OnTrainReachedTrackEnd.Invoke(trainId);
        }

        return Result.Success();
    }
}
