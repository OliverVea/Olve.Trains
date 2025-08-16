using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Vehicles;

public class Vehicle(Id<Vehicle> id, string name) : IHasId<Id<Vehicle>>
{
    private readonly string _toString = $"{name} ({id.Value})";

    public Id<Vehicle> Id { get; } = id;
    public string Name { get; set; } = name;
    
    public override string ToString()
    {
        return _toString;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public bool Equals(Vehicle? other)
    {
        return other != null && Id == other.Id;
    }
}