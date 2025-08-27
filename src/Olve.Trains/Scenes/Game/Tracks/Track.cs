using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Tracks;

public readonly record struct Track(Id<Track> Id, TrackPoint Start, TrackPoint End) : IHasId<Id<Track>>;