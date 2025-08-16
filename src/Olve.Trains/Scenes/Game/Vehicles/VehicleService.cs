using Olve.Engine3D.Systems;
using Olve.Logging;
using Olve.Results;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class VehicleService(ILoggingManager loggingManager) : BaseEntityService<Vehicle>(loggingManager)
{
    public Result<Id<Vehicle>> AddVehicle(string name)
    {
        var vehicleId = Id<Vehicle>.New();
        Vehicle vehicle = new(vehicleId, name);
        return Add(vehicle);
    }
}