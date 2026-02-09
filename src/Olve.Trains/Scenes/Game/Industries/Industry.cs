using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Industries;

public readonly record struct Industry(Id<Industry> Id, Id<Building> BuildingId) : IHasId<Id<Industry>>;