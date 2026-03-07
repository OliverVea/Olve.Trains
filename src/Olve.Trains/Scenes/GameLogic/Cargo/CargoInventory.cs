using System.Collections.Immutable;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public readonly record struct CargoInventory(
    Id<CargoInventory> Id,
    int Capacity,
    ImmutableDictionary<Id<CargoType>, int>? AllowedTypes) : IHasId<Id<CargoInventory>>;
