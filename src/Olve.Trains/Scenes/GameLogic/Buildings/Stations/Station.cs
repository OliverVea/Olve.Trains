using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Buildings.Stations;

public readonly record struct Station(Id<Building> BuildingId, Id<Track> TrackId);