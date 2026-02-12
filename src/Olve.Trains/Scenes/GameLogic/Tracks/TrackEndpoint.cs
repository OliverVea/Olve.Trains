using System.Runtime.InteropServices;

namespace Olve.Trains.Scenes.Game.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrackEndpoint(Vector3D<float> Point, Vector3D<float> Tangent);