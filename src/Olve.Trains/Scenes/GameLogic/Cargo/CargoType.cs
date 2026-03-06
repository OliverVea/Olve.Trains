using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Cargo;

public readonly record struct CargoType(Id<CargoType> Id, string Name) : IHasId<Id<CargoType>>;
