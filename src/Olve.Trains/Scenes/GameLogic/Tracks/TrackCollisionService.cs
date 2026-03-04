using Olve.Engine3D.Physics3D.Collisions;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackCollisionService(
    CollisionSystem collisionSystem,
    TrackSplineService trackSplineService)
{
    private const int SegmentCount = 10;
    private const float TrackWidth = 0.16f;
    private const float TrackHeight = 0.02f;

    private static readonly BoxColliderShape HalfUnitBox = new(new(0.5f, 0.5f, 0.5f));

    private readonly Dictionary<Id<Track>, (Id<Collider>[] ColliderIds, Matrix4X4<float>[] Matrices)> _trackColliders = new();
    private readonly Dictionary<Id<Collider>, Id<Track>> _colliderToTrack = new();

    public Result Register(Id<Track> trackId)
    {
        if (_trackColliders.ContainsKey(trackId))
        {
            return new ResultProblem("Track colliders already exist for track '{0}'", trackId);
        }

        if (trackSplineService.GetPoints(trackId, SegmentCount + 1)
            .TryPickProblems(out var problems, out var points))
        {
            return problems.Prepend("Failed to sample spline for track '{0}'", trackId);
        }

        var pointArray = points.ToArray();
        var colliderIds = new Id<Collider>[SegmentCount];
        var matrices = new Matrix4X4<float>[SegmentCount];

        for (var i = 0; i < SegmentCount; i++)
        {
            var from = pointArray[i];
            var to = pointArray[i + 1];
            var matrix = ComputeSegmentOBBMatrix(from, to);
            matrices[i] = matrix;

            var colliderId = collisionSystem.Register(HalfUnitBox, ColliderGroups.Track, matrix);
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

    private static Matrix4X4<float> ComputeSegmentOBBMatrix(Vector3D<float> from, Vector3D<float> to)
    {
        var midpoint = (from + to) * 0.5f;
        var direction = to - from;
        var length = direction.Length;

        if (length < 1e-6f)
        {
            return Matrix4X4.CreateTranslation(midpoint);
        }

        var forward = Vector3D.Normalize(direction);
        var up = new Vector3D<float>(0, 1, 0);
        var right = Vector3D.Normalize(Vector3D.Cross(up, forward));

        // Re-orthogonalize up in case forward is near-vertical
        up = Vector3D.Cross(forward, right);

        // Scale: X = width, Y = height, Z = segment length
        var scale = Matrix4X4.CreateScale(TrackWidth, TrackHeight, length);

        // Rotation matrix from basis vectors (row-major: rows are axes)
        var rotation = new Matrix4X4<float>(
            right.X, right.Y, right.Z, 0,
            up.X, up.Y, up.Z, 0,
            forward.X, forward.Y, forward.Z, 0,
            0, 0, 0, 1);

        var translation = Matrix4X4.CreateTranslation(midpoint);

        return scale * rotation * translation;
    }
}
