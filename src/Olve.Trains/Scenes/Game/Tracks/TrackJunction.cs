using Olve.Engine3D;
using Olve.Utilities.Ids;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Tracks;

public readonly record struct TrackJunction(Id<TrackJunction> Id, TilePosition Position) : IHasId<Id<TrackJunction>>;