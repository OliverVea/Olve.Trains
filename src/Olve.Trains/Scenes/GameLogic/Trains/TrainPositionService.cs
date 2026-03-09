using System.Collections.Concurrent;
using Olve.Engine3D.Scenes;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehiclePositionService(VehicleService vehicleService) : ISceneService
{
    private readonly ConcurrentDictionary<Id<Vehicle>, VehiclePositionType> _positionTypes = new();
    private readonly ConcurrentDictionary<Id<Vehicle>, VehicleTrackPosition> _trackPositions = new();

    public Result Load()
    {
        vehicleService.OnVehicleRemoved.Subscribe(OnRemoved);
        return Result.Success();
    }

    public Result Unload()
    {
        vehicleService.OnVehicleRemoved.Unsubscribe(OnRemoved);
        return Result.Success();
    }

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

    private void OnRemoved(Id<Vehicle> id)
    {
        _positionTypes.Remove(id, out _);
        _trackPositions.Remove(id, out _);
    }
}