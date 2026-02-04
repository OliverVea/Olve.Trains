using Olve.Engine3D.Scenes;
using Olve.Engine3D.Systems;
using Olve.Engine3D.Utilities;
using Olve.Logging;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleMovementService(ILoggingManager loggingManager,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService) : SceneService(loggingManager)
{
    public Event<Id<Vehicle>> OnVehicleReachedTrackEnd { get; } = new();
    
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

    private Result UpdateTrackPosition(Id<Vehicle> vehicleId, TimeSpan deltaTime, VehicleTrackPosition vehicleTrackPosition)
    {
        if (trackSplineService.GetLength(vehicleTrackPosition.TrackId).TryPickProblems(out var problems, out var trackLength))
        {
            return problems;
        }
                
        var newTime = vehicleTrackPosition.Time + vehicleTrackPosition.Velocity * deltaTime.InSeconds() / trackLength;

        var reachedEndOfTrack = newTime < 0 && vehicleTrackPosition.Velocity < 0 || newTime > 1 && vehicleTrackPosition.Velocity > 0;
        newTime = float.Clamp(newTime, 0, 1);
        
        vehicleTrackPosition = vehicleTrackPosition with { Time = newTime };
        vehiclePositionService.SetTrackPosition(vehicleId, vehicleTrackPosition);

        if (reachedEndOfTrack)
        {
            OnVehicleReachedTrackEnd.Invoke(vehicleId);
        }
        
        return Result.Success();
    }
}