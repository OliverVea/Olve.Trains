using System.Runtime.InteropServices;
using Olve.Trains.Scenes.Game.Tracks;

namespace Olve.Trains.Scenes.Game.Vehicles;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct VehicleTrackPosition(TrackPoint TrackPoint, float Velocity)
{
    public Id<Track> TrackId => TrackPoint.TrackId;
    public float Time => TrackPoint.Time;
}