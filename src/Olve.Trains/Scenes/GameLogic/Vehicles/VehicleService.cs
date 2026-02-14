using Olve.Engine3D.Systems;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleService
{
    private readonly EntityStore<Vehicle> _vehicles = new();
    private int _count;

    public Event<Id<Vehicle>> OnVehicleAdded => _vehicles.OnAdded;
    public Event<Id<Vehicle>> OnVehicleRemoved => _vehicles.OnRemoved;

    public int Count => _count;

    public Result<Id<Vehicle>> AddVehicle(string name)
    {
        var vehicleId = Id.New<Vehicle>();
        Vehicle vehicle = new(vehicleId, name);
        if (!_vehicles.TryAdd(vehicle))
        {
            return new ResultProblem("Vehicle already exists: '{0}'", vehicleId);
        }

        _count++;
        return vehicleId;
    }
}
