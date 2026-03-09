using System.Collections.Immutable;
using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public readonly record struct WagonBlueprint(
    Id<WagonBlueprint> Id,
    string Name,
    int Capacity,
    ImmutableHashSet<Id<CargoType>>? AllowedTypes,
    float Length) : IHasId<Id<WagonBlueprint>>;
