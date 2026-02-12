using System.Runtime.InteropServices;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct JunctionConnection(Id<Track> TrackId, TrackEndpoint TrackEndpoint);