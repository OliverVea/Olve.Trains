using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Collision;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackValidationService(
    TrackSplineService trackSplineService,
    CollisionSystem collisionSystem)
{
    public const int SamplingPoints = 25;
    public const float MaxCurvature = 1f;

    private readonly HashSet<Id<ColliderGroup>> _colliderGroups = [ColliderGroups.Building, ColliderGroups.Environment];

    public bool IsValid(TrackEndpoint from, TrackEndpoint to) =>
        !HasInvalidCurvature(from, to) && !HasCollisions(from, to);

    private bool HasInvalidCurvature(TrackEndpoint from, TrackEndpoint to) =>
        trackSplineService
            .CreateSpline(from, to)
            .GetCurvatures(SamplingPoints)
            .Map(x => x.Any(c => float.Abs(c) > MaxCurvature))
            .TryPickProblems(out _, out var hasInvalid) || hasInvalid;

    private bool HasCollisions(TrackEndpoint from, TrackEndpoint to)
    {
        var spline = trackSplineService.CreateSpline(from, to);

        if (spline.GetPoints(TrackSegmentHelper.SegmentCount + 1)
            .TryPickProblems(out _, out var points))
        {
            return false;
        }

        var pointArray = points.ToArray();

        // TODO: Do narrow-phase query instead of just AABB-based.
        for (var i = 0; i < TrackSegmentHelper.SegmentCount; i++)
        {
            var segmentFrom = pointArray[i];
            var segmentTo = pointArray[i + 1];
            var matrix = TrackSegmentHelper.ComputeSegmentOBBMatrix(segmentFrom, segmentTo);
            var segmentAABB = TrackSegmentHelper.HalfUnitBox.GetWorldAABB(matrix);

            if (collisionSystem.QueryOverlapAABB(segmentAABB, _colliderGroups).Any())
            {
                return true;
            }
        }

        return false;
    }
}
