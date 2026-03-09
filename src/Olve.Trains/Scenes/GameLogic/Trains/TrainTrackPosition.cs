using System.Runtime.InteropServices;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct VehicleTrackPosition(TrackPoint TrackPoint, float Velocity)
{
    public Id<Track> TrackId => TrackPoint.TrackId;
    public float Time => TrackPoint.Time;
}