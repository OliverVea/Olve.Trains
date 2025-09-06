using System.Runtime.InteropServices;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrackPosition(Id<Track> TrackId, float Time, float Velocity);