using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

public readonly record struct TrackPosition(Id<Track> TrackId, float Time, float Velocity);