using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.Ids;

namespace Olve.Trains.Scenes.Game.Vehicles;

public readonly record struct TrackPosition(Id<Track> TrackId, float Time, float Velocity);