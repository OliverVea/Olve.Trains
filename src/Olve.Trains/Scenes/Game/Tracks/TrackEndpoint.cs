using System.Runtime.InteropServices;
using Olve.Engine3D;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Game.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrackEndpoint(Vector3D<float> Point, Vector3D<float> Tangent);

public static class TrackPointExtensions
{
    public static bool IsOnStraightLineWith(this TrackEndpoint endpoint, TrackEndpoint otherEndpoint)
    {
        var delta = otherEndpoint.Point - endpoint.Point;

        return delta.CountZeroDimensions() == 2
               && (endpoint.Tangent - otherEndpoint.Tangent).Length < MathConstants.Epsilon
               && (Vector3D.Normalize(endpoint.Tangent) - Vector3D.Normalize(delta)).Length < MathConstants.Epsilon;
    }
}