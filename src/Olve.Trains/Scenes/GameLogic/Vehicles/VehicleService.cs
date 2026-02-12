using Olve.Engine3D.Systems;
using Microsoft.Extensions.Logging.Abstractions;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleService() : BaseEntityService<Vehicle>(NullLogger.Instance)
{
    public Result<Id<Vehicle>> AddVehicle(string name)
    {
        var vehicleId = Id.New<Vehicle>();
        Vehicle vehicle = new(vehicleId, name);
        return Add(vehicle);
    }
}