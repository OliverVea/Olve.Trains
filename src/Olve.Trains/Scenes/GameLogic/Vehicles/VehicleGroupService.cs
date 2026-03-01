using Olve.Utilities.Collections;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleGroupService
{
    private readonly ManyToManyLookup<Id<VehicleGroup>, Id<Vehicle>> _membership = new();

    public void AddToGroup(Id<Vehicle> vehicleId, Id<VehicleGroup> groupId)
    {
        _membership.Set(groupId, vehicleId, true);
    }

    public void RemoveFromGroup(Id<Vehicle> vehicleId, Id<VehicleGroup> groupId)
    {
        _membership.Set(groupId, vehicleId, false);
    }

    public bool IsMemberOf(Id<Vehicle> vehicleId, Id<VehicleGroup> groupId)
    {
        return _membership.Contains(groupId, vehicleId);
    }
}
