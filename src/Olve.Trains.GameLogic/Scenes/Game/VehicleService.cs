using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game;

public class Vehicle
{
    public Vehicle(Id<Vehicle> id, Id<Track> trackId, float position)
    {
        Id = id;
        TrackId = trackId;
        Position = position;
    }

    public Id<Vehicle> Id { get; }
    public Id<Track> TrackId { get; internal set; }
    public float Position { get; internal set; }
}

public class VehicleService(TrackService trackService)
{
    private readonly Dictionary<Id<Vehicle>, Vehicle> _vehicles = new();

    public Action<Vehicle>? OnVehicleAdded { get; set; }
    public Action<Vehicle>? OnVehicleRemoved { get; set; }
    public Action<Vehicle, Id<Track>, Id<Track>>? OnVehicleTrackChanged { get; set; }

    public Result<Id<Vehicle>> AddVehicle(Id<Track> trackId, float position)
    {
        if (!trackService.TryGetTrack(trackId, out _))
        {
            return new ResultProblem("Track with id '{0}' not found", trackId);
        }

        var id = Id<Vehicle>.New();
        Vehicle vehicle = new(id, trackId, position);

        _vehicles.Add(id, vehicle);
        OnVehicleAdded?.Invoke(vehicle);

        return id;
    }

    public Result RemoveVehicle(Id<Vehicle> vehicleId)
    {
        if (!_vehicles.TryGetValue(vehicleId, out var vehicle))
        {
            return new ResultProblem("Vehicle with id '{0}' not found", vehicleId);
        }

        _vehicles.Remove(vehicleId);
        OnVehicleRemoved?.Invoke(vehicle);

        return Result.Success();
    }

    public Result UpdateVehiclePosition(Id<Vehicle> vehicleId, Id<Track> trackId, float position)
    {
        if (!_vehicles.TryGetValue(vehicleId, out var vehicle))
        {
            return new ResultProblem("Vehicle with id '{0}' not found", vehicleId);
        }

        if (!trackService.TryGetTrack(trackId, out _))
        {
            return new ResultProblem("Track with id '{0}' not found", trackId);
        }

        var previousTrackId = vehicle.TrackId;
        vehicle.TrackId = trackId;
        vehicle.Position = position;

        if (previousTrackId != trackId)
        {
            OnVehicleTrackChanged?.Invoke(vehicle, previousTrackId, trackId);
        }

        return Result.Success();
    }

    public bool TryGetVehicle(Id<Vehicle> vehicleId, out Vehicle vehicle)
    {
        return _vehicles.TryGetValue(vehicleId, out vehicle);
    }
}

