using Olve.Engine3D;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Industries;

public readonly record struct Building(Id<Building> Id, TilePosition Origin, TileFootprint Footprint) : IHasId<Id<Building>>;