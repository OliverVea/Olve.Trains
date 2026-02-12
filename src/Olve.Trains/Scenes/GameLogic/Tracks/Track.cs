using System.Runtime.InteropServices;
using Olve.Utilities.Lookup;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct Track(Id<Track> Id, TrackEndpoint Start, TrackEndpoint End) : IHasId<Id<Track>>;