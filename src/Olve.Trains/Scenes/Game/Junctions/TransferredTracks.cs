using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Junctions;

public readonly record struct TransferredTracks(Id<Track> From, Id<Track> To);