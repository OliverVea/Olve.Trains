using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public readonly record struct VehicleGroup(Id<VehicleGroup> Id, string Name) : IHasId<Id<VehicleGroup>>;