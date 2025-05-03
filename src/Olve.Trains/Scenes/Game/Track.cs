using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game;

public readonly record struct Track(Id<Track> Id, TrackPoint Start, TrackPoint End);