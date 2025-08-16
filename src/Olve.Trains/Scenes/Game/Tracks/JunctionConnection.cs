using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Tracks;

public readonly record struct JunctionConnection(Id<Track> TrackId, TrackPoint TrackPoint);