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
            var result = UpdateTrackPosition(trainId, deltaTime, trackPosition);
            if (result.TryPickProblems(out var problems))
            {
                logger.LogWarning("Failed to update track position: {Problems}", problems);
            }
        }

        return Result.Success();
    }

    private Result UpdateTrackPosition(Id<Train> trainId, TimeSpan deltaTime, TrainTrackPosition trainTrackPosition)
    {
        if (trackSplineService.GetLength(trainTrackPosition.TrackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }

        var newTime = trainTrackPosition.Time + trainTrackPosition.Velocity * deltaTime.InSeconds() / trackLength;

        var reachedEndOfTrack = newTime < 0 && trainTrackPosition.Velocity < 0 || newTime > 1 && trainTrackPosition.Velocity > 0;
        newTime = float.Clamp(newTime, 0, 1);

        var newTrackPoint = trainTrackPosition.TrackPoint with { Time = newTime };
        trainTrackPosition = trainTrackPosition with { TrackPoint = newTrackPoint };
        trainPositionService.SetTrackPosition(trainId, trainTrackPosition);

        if (reachedEndOfTrack)
        {
            OnTrainReachedTrackEnd.Invoke(trainId);
        }

        return Result.Success();
    }
}