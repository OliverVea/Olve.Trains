using Olve.Utilities.Lookup;
using Olve.Trains.Scenes.GameLogic.Ordering;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class Train(Id<Train> id, string name, long createdSequence) : IHasId<Id<Train>>, IHasCreatedSequence, IEquatable<Train?>
{
    private readonly string _toString = $"{name} ({id.Value})";

    public Id<Train> Id { get; } = id;
    public string Name { get; set; } = name;
    public long CreatedSequence { get; } = createdSequence;
    
    public override string ToString() => _toString;
    public override int GetHashCode() => Id.GetHashCode();
    public bool Equals(Train? other) => other != null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as Train);
}