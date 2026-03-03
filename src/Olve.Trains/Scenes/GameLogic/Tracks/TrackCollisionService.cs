using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Rendering.Primitives;

namespace Olve.Trains.Scenes.GameLogic.Tracks;

public class TrackCollisionService(
    MeshManager meshManager,
    CollisionSystem collisionSystem,
    TrackSplineService trackSplineService)
{
    private const int SegmentCount = 10;
    private const float TrackWidth = 0.16f;
    private const float TrackHeight = 0.02f;

    private readonly Dictionary<Id<Track>, (Id<Collider>[] ColliderIds, Matrix4X4<float>[] Matrices)> _trackColliders = new();
    private readonly Dictionary<Id<Collider>, Id<Track>> _colliderToTrack = new();
    private Id<Mesh> _unitCubeMeshId;
    private bool _initialized;

    private Result EnsureInitialized()
    {
        if (_initialized) return Result.Success();

        var meshData = CreateCenteredCubeMeshData();

        if (meshManager.Register(meshData)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to register unit cube mesh for track collision");
        }

        _unitCubeMeshId = meshId;
        _initialized = true;
        return Result.Success();
    }

    public Result Register(Id<Track> trackId)
    {
        if (EnsureInitialized().TryPickProblems(out var initProblems))
        {
            return initProblems;
        }

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

            if (collisionSystem.RegisterMeshCollider(_unitCubeMeshId, ColliderGroups.Track, matrix)
                .TryPickProblems(out problems, out var colliderId))
            {
                return problems.Prepend("Failed to register collider for track '{0}' segment {1}", trackId, i);
            }

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

    private static MeshData CreateCenteredCubeMeshData()
    {
        var positions = new Vector3D<float>[UnitCube.VertexCount];
        var normals = new Vector3D<float>[UnitCube.VertexCount];
        UnitCube.GetVertices(positions, normals);

        // Center the cube: shift from (0,0,0)→(1,1,1) to (-0.5,-0.5,-0.5)→(0.5,0.5,0.5)
        var offset = new Vector3D<float>(0.5f, 0.5f, 0.5f);
        for (var i = 0; i < positions.Length; i++)
        {
            positions[i] -= offset;
        }

        var indices = new uint[UnitCube.IndexCount];
        UnitCube.GetIndices(indices);

        var triangleIndices = new TriangleIndex[UnitCube.IndexCount / 3];
        for (var i = 0; i < triangleIndices.Length; i++)
        {
            triangleIndices[i] = new TriangleIndex(indices[i * 3], indices[i * 3 + 1], indices[i * 3 + 2]);
        }

        return new MeshData
        {
            Positions = positions,
            Normals = normals,
            Indices = triangleIndices,
            TextureCoordinates = new Vector2D<float>[UnitCube.VertexCount],
        };
    }
}
