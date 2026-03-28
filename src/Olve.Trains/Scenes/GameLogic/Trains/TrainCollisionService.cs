using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Trains;

public class TrainCollisionService(
    MeshLoadingManager meshLoadingManager,
    MeshManager meshManager,
    CollisionSystem collisionSystem,
    TrainPositionService trainPositionService,
    TrackSplineService trackSplineService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackSplineService, trainPositionService]);

    private readonly Dictionary<Id<Train>, Id<Collider>> _colliders = new();
    private readonly Dictionary<Id<Collider>, Id<Train>> _reverseColliders = new();
    private BoxColliderShape? _shape;
    private Vector3D<float> _centerOffset;

    public Result Load()
    {
        if (meshLoadingManager.LoadMesh(Meshes.SM_Veh_Freight_01)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load train mesh for collision");
        }

        if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
        {
            return new ResultProblem("Mesh AABB not found for train mesh");
        }

        var scale = TrainWorldMatrix.TrainScale;
        var halfExtents = (localAABB.Max - localAABB.Min) * 0.5f * scale;
        _centerOffset = (localAABB.Min + localAABB.Max) * 0.5f * scale;
        _shape = new BoxColliderShape(halfExtents);

        return Result.Success();
    }

    public Result Register(Id<Train> trainId)
    {
        if (_colliders.ContainsKey(trainId))
        {
            return new ResultProblem("Collider already exists for train '{0}'", trainId);
        }

        var centerMatrix = Matrix4X4.CreateTranslation(_centerOffset);
        var colliderId = collisionSystem.Register(_shape!, ColliderGroups.Train, centerMatrix);

        _colliders[trainId] = colliderId;
        _reverseColliders[colliderId] = trainId;
        return Result.Success();
    }

    public Result Unregister(Id<Train> trainId)
    {
        if (!_colliders.Remove(trainId, out var colliderId))
        {
            return Result.Success();
        }

        _reverseColliders.Remove(colliderId);
        return collisionSystem.Unregister(colliderId).MapToResult();
    }

    public bool TryGetTrainId(Id<Collider> colliderId, out Id<Train> trainId) =>
        _reverseColliders.TryGetValue(colliderId, out trainId);

    public Result Update()
    {
        foreach (var (trainId, trackPosition) in trainPositionService.TrackPositions)
        {
            if (!_colliders.TryGetValue(trainId, out var colliderId))
            {
                continue;
            }

            if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time)
                .TryPickProblems(out var problems, out var position))
            {
                return problems.Prepend(
                    "Failed to sample spline for train '{0}' collision update", trainId);
            }

            var worldMatrix = TrainWorldMatrix.Compute(trackPosition.Direction, position);
            var centerMatrix = Matrix4X4.CreateTranslation(_centerOffset);
            var adjustedMatrix = centerMatrix * worldMatrix;

            if (collisionSystem.UpdateTransform(colliderId, adjustedMatrix)
                .TryPickProblems(out problems))
            {
                return problems.Prepend(
                    "Failed to update collider transform for train '{0}'", trainId);
            }
        }

        return Result.Success();
    }
}
