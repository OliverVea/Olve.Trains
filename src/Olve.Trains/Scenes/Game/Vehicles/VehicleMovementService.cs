using Olve.Engine3D;
using Olve.Engine3D.Scenes;
using Olve.Logging;
using Olve.Results;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.CollectionExtensions;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleMovementService(ILoggingManager loggingManager,
    VehiclePositionService vehiclePositionService,
    TrackService trackService,
    TrackConnectionService trackConnectionService,
    TrackSplineService trackSplineService) : SceneService(loggingManager)
{
    protected override Result OnUpdate(TimeSpan deltaTime)
    {
        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            var result = UpdateTrackPosition(vehicleId, deltaTime, trackPosition);
            if (result.TryPickProblems(out var problems))
            {
                LoggingManager.Log(problems);
            }
        }

        return Result.Success();
    }

    private Result UpdateTrackPosition(Id<Vehicle> vehicleId, TimeSpan deltaTime, TrackPosition trackPosition)
    {
        if (trackSplineService.GetLength(trackPosition.TrackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }
                
        var newTime = trackPosition.Time + trackPosition.Velocity * deltaTime.InSeconds() / trackLength;

        var trackShift = newTime < 0 && trackPosition.Velocity < 0
            ? -1
            : newTime > 1 && trackPosition.Velocity > 0
                ? 1
                : 0;
        
        if (trackShift == 0)
        {
            trackPosition = trackPosition with { Time = newTime };
            vehiclePositionService.SetTrackPosition(vehicleId, trackPosition);
            return Result.Success();
        }

        if (!trackService.TryGet(trackPosition.TrackId, out var track))
        {
            return new ResultProblem("Could not find track with id '{0}'",  trackPosition.TrackId);
        }

        var endPosition = trackShift == -1 ? track.Start.Point : track.End.Point;
        var endTangent = trackShift == -1 ? track.Start.Tangent : track.End.Tangent;

        TrackPoint endPoint = new(endPosition, endTangent);

        var connectingTrackIds = trackConnectionService.GetConnectingTracks(endPoint);
        if (connectingTrackIds.Count == 0)
        {
            return Result.Success();
        }

        var connectingTrackId = connectingTrackIds.PickRandom();
        if (!trackService.TryGet(connectingTrackId, out var connectingTrack))
        {
            return new ResultProblem("Could not find connecting track with id '{0}'",  trackPosition.TrackId);
        }

        var isConnectingTrackStart = (connectingTrack.Start.Point - endPosition).LengthSquared <
                                     (connectingTrack.End.Point - endPosition).LengthSquared;

        newTime = trackShift == -1 ? -newTime : newTime - 1;
        newTime = isConnectingTrackStart ? newTime : 1 - newTime;
        newTime = float.Clamp(newTime, 0, 1);
        
        var newVelocity = float.Abs(trackPosition.Velocity);
        newVelocity *= isConnectingTrackStart ? 1 : -1;

        TrackPosition newTrackPosition = new(connectingTrackId, newTime, newVelocity);
        vehiclePositionService.SetTrackPosition(vehicleId, newTrackPosition);
        
        return Result.Success();
    }
}