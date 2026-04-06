using Olve.Trains.Scenes.GameLogic.Buildings;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Cities;

public readonly record struct Residence(
    Id<Residence> Id,
    Id<Building> BuildingId,
    Id<City> CityId) : IHasId<Id<Residence>>;
