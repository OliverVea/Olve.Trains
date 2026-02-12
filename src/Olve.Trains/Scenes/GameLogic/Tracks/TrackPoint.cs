using System.Runtime.InteropServices;

namespace Olve.Trains.Scenes.Game.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrackPoint(Id<Track> TrackId, float Time);
