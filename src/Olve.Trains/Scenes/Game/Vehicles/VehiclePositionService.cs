using System.Collections.Concurrent;
using Olve.Engine3D.Systems;
using Olve.Logging;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehiclePositionService(ILoggingManager loggingManager,
    VehicleService vehicleService) : BaseEntityAuxiliaryService<Vehicle>(loggingManager, vehicleService)
{
    private readonly ConcurrentDictionary<Id<Vehicle>, VehiclePositionType> _positionTypes = new();
    private readonly ConcurrentDictionary<Id<Vehicle>, VehicleTrackPosition> _trackPositions = new();

    public IEnumerable<(Id<Vehicle>, VehicleTrackPosition)> TrackPositions => _trackPositions.Select(x => (x.Key, x.Value));

    public VehiclePositionType GetPositionType(Id<Vehicle> vehicleId)
    {
        return _positionTypes.GetValueOrDefault(vehicleId, VehiclePositionType.None);
    }

    public Result SetTrackPosition(Id<Vehicle> vehicleId, VehicleTrackPosition vehicleTrackPosition)
    {
        _positionTypes[vehicleId] = VehiclePositionType.OnTrack;
        _trackPositions[vehicleId] = vehicleTrackPosition;
        return Result.Success();
    }

    public bool TryGetTrackPosition(Id<Vehicle> vehicleId, out VehicleTrackPosition vehicleTrackPosition)
    {
        return _trackPositions.TryGetValue(vehicleId, out vehicleTrackPosition);
    }

    protected override void OnRemoved(Id<Vehicle> id)
    {
        _positionTypes.Remove(id, out _);
        _trackPositions.Remove(id, out _);
    }
}