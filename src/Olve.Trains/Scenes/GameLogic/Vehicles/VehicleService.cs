using Olve.Engine3D.Diagnostics;
using Olve.Engine3D.Systems;
using Olve.Trains.Shared.Telemetry;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleService(EntityStoreFactory entityStoreFactory)
{
    private readonly EntityStore<Vehicle> _vehicles = entityStoreFactory.Create<Vehicle>();
    private int _count;

    public Event<Id<Vehicle>> OnVehicleAdded => _vehicles.OnAdded;
    public Event<Id<Vehicle>> OnVehicleRemoved => _vehicles.OnRemoved;
    public IEnumerable<Id<Vehicle>> VehicleIds => _vehicles.Keys;

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
        if (EngineMetrics.IsEnabled) GameMetrics.VehicleCount.Add(1);
        return vehicleId;
    }

    public DeletionResult DeleteVehicle(Id<Vehicle> vehicleId)
    {
        var result = _vehicles.Remove(vehicleId);
        if (!result.WasNotFound)
        {
            _count--;
            if (EngineMetrics.IsEnabled) GameMetrics.VehicleCount.Add(-1);
        }
        return result;
    }
}
