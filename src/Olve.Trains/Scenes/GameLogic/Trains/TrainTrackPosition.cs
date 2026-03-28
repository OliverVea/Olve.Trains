using System.Runtime.InteropServices;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrainTrackPosition(TrackPoint TrackPoint, TrainDirection Direction)
{
    public Id<Track> TrackId => TrackPoint.TrackId;
    public float Time => TrackPoint.Time;
}
