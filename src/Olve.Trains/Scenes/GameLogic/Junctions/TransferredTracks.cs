using System.Runtime.InteropServices;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Junctions;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TransferredTracks(Id<Track> From, Id<Track> To);