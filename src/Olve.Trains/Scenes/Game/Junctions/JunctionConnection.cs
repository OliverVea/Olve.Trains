using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Junctions;

public readonly record struct JunctionConnection(Id<Track> TrackId, TrackPoint TrackPoint);