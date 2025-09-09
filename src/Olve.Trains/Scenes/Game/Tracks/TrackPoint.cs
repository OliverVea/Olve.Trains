using System.Runtime.InteropServices;
using Olve.Engine3D;
using Silk.NET.OpenGL;

namespace Olve.Trains.Scenes.Game.Tracks;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TrackPoint(Vector3D<float> Point, Vector3D<float> Tangent);

public static class TrackPointExtensions
{
    public static bool IsOnStraightLineWith(this TrackPoint point, TrackPoint otherPoint)
    {
        var delta = otherPoint.Point - point.Point;
        
        return delta.CountZeroDimensions() == 2
               && (point.Tangent - otherPoint.Tangent).Length < MathConstants.Epsilon
               && (Vector3D.Normalize(point.Tangent) - Vector3D.Normalize(delta)).Length < MathConstants.Epsilon;
    }
}