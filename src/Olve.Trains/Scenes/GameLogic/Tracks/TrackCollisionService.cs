using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Collision;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackCollisionService(
    CollisionSystem collisionSystem,
    TrackSplineService trackSplineService)
{
    private readonly Dictionary<Id<Track>, (Id<Collider>[] ColliderIds, Matrix4X4<float>[] Matrices)> _trackColliders = new();
    private readonly Dictionary<Id<Collider>, Id<Track>> _colliderToTrack = new();

    public Result Register(Id<Track> trackId)
    {
        if (_trackColliders.ContainsKey(trackId))
        {
            return new ResultProblem("Track colliders already exist for track '{0}'", trackId);
        }

        if (trackSplineService.GetPoints(trackId, TrackSegmentHelper.SegmentCount + 1)
            .TryPickProblems(out var problems, out var points))
        {
            return problems.Prepend("Failed to sample spline for track '{0}'", trackId);
        }

        var pointArray = points.ToArray();
        var colliderIds = new Id<Collider>[TrackSegmentHelper.SegmentCount];
        var matrices = new Matrix4X4<float>[TrackSegmentHelper.SegmentCount];

        for (var i = 0; i < TrackSegmentHelper.SegmentCount; i++)
        {
            var from = pointArray[i];
            var to = pointArray[i + 1];
            var matrix = TrackSegmentHelper.ComputeSegmentOBBMatrix(from, to);
            matrices[i] = matrix;

            var colliderId = collisionSystem.Register(TrackSegmentHelper.HalfUnitBox, ColliderGroups.Track, matrix);
            colliderIds[i] = colliderId;
            _colliderToTrack[colliderId] = trackId;
        }

        _trackColliders[trackId] = (colliderIds, matrices);
        return Result.Success();
    }

    public Result Unregister(Id<Track> trackId)
    {
        if (!_trackColliders.Remove(trackId, out var entry))
        {
            return Result.Success();
        }

        foreach (var colliderId in entry.ColliderIds)
        {
            _colliderToTrack.Remove(colliderId);
            collisionSystem.Unregister(colliderId);
        }

        return Result.Success();
    }

    public bool TryGetTrackId(Id<Collider> colliderId, out Id<Track> trackId)
    {
        return _colliderToTrack.TryGetValue(colliderId, out trackId);
    }

    public bool TryGetMatrices(Id<Track> trackId, out Matrix4X4<float>[] matrices)
    {
        if (_trackColliders.TryGetValue(trackId, out var entry))
        {
            matrices = entry.Matrices;
            return true;
        }

        matrices = [];
        return false;
    }

}
