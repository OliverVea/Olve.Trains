using System.Collections.Concurrent;
using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Vehicles;

public enum VehiclePositionType
{
    None = 0,
    OnTrack
}

public class VehiclePositionService(ILoggingManager loggingManager, VehicleService vehicleService) : BaseEntityAuxiliaryService<VehicleService, Vehicle>(loggingManager, vehicleService)
{
    private readonly ConcurrentDictionary<Id<Vehicle>, VehiclePositionType> _positionTypes = new();
    private readonly ConcurrentDictionary<Id<Vehicle>, TrackPosition> _trackPositions = new();

    public IEnumerable<(Id<Vehicle>, TrackPosition)> TrackPositions => _trackPositions.Select(x => (x.Key, x.Value));

    public VehiclePositionType GetPositionType(Id<Vehicle> vehicleId)
    {
        return _positionTypes.GetValueOrDefault(vehicleId, VehiclePositionType.None);
    }

    public void SetTrackPosition(Id<Vehicle> vehicleId, TrackPosition trackPosition)
    {
        _positionTypes[vehicleId] = VehiclePositionType.OnTrack;
        _trackPositions[vehicleId] = trackPosition;
    }

    public bool TryGetTrackPosition(Id<Vehicle> vehicleId, out TrackPosition trackPosition)
    {
        return _trackPositions.TryGetValue(vehicleId, out trackPosition);
    }

    protected override void OnRemoved(Id<Vehicle> id)
    {
        _positionTypes.Remove(id, out _);
        _trackPositions.Remove(id, out _);
    }
}