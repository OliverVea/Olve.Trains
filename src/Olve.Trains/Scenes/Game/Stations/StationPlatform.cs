using System.Runtime.InteropServices;
using Olve.Trains.Scenes.Game.Tracks;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.Game.Stations;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct StationPlatform(Id<StationPlatform> Id, Id<Track> TrackId, Id<Station> StationId) : IHasId<Id<StationPlatform>>;