using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class Vehicle(Id<Vehicle> id, string name) : IHasId<Id<Vehicle>>, IEquatable<Vehicle?>
{
    private readonly string _toString = $"{name} ({id.Value})";

    public Id<Vehicle> Id { get; } = id;
    public string Name { get; set; } = name;
    
    public override string ToString() => _toString;
    public override int GetHashCode() => Id.GetHashCode();
    public bool Equals(Vehicle? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as Vehicle);
}