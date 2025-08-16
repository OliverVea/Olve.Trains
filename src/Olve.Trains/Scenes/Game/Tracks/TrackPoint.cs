using Silk.NET.Maths;

namespace Olve.Trains.Scenes.Game.Tracks;

public readonly record struct TrackPoint(Vector3D<float> Point, Vector3D<float> Tangent);