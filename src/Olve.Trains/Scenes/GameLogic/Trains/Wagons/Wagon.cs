using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Trains.Wagons;

public readonly record struct Wagon(Id<Wagon> Id, Id<WagonBlueprint> BlueprintId) : IHasId<Id<Wagon>>;
