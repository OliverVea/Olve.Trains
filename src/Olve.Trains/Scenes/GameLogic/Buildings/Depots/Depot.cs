using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Depots;

public readonly record struct Depot(Id<Building> BuildingId, Id<Track> TrackId);
