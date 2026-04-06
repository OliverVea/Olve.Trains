using Olve.Trains.Scenes.GameLogic.Cargo;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Cities;

public readonly record struct City(
    Id<City> Id,
    Id<CargoInventory> InventoryId) : IHasId<Id<City>>;
