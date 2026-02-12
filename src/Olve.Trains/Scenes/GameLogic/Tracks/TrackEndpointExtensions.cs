using Olve.Engine3D;
using Olve.Engine3D.Utilities;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public static class TrackEndpointExtensions
{
    public static bool IsOnStraightLineWith(this TrackEndpoint endpoint, TrackEndpoint otherEndpoint)
    {
        var delta = otherEndpoint.Point - endpoint.Point;

        return delta.CountZeroDimensions() == 2
               && (endpoint.Tangent - otherEndpoint.Tangent).Length < MathConstants.Epsilon
               && (Vector3D.Normalize(endpoint.Tangent) - Vector3D.Normalize(delta)).Length < MathConstants.Epsilon;
    }
}