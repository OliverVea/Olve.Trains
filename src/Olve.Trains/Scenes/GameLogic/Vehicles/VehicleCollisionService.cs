using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleCollisionService(
    MeshLoadingManager meshLoadingManager,
    MeshManager meshManager,
    CollisionSystem collisionSystem,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackSplineService, vehiclePositionService]);

    private readonly Dictionary<Id<Vehicle>, Id<Collider>> _colliders = new();
    private BoxColliderShape? _shape;
    private Vector3D<float> _centerOffset;

    public Result Load()
    {
        if (meshLoadingManager.LoadMesh(Meshes.SM_Veh_Bullet_01)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load vehicle mesh for collision");
        }

        if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
        {
            return new ResultProblem("Mesh AABB not found for vehicle mesh");
        }

        var halfExtents = (localAABB.Max - localAABB.Min) * 0.5f;
        _centerOffset = (localAABB.Min + localAABB.Max) * 0.5f;
        _shape = new BoxColliderShape(halfExtents);

        return Result.Success();
    }

    public Result Register(Id<Vehicle> vehicleId)
    {
        if (_colliders.ContainsKey(vehicleId))
        {
            return new ResultProblem("Collider already exists for vehicle '{0}'", vehicleId);
        }

        var centerMatrix = Matrix4X4.CreateTranslation(_centerOffset);
        var colliderId = collisionSystem.Register(_shape!, ColliderGroups.Vehicle, centerMatrix);

        _colliders[vehicleId] = colliderId;
        return Result.Success();
    }

    public Result Unregister(Id<Vehicle> vehicleId)
    {
        if (!_colliders.Remove(vehicleId, out var colliderId))
        {
            return Result.Success();
        }

        return collisionSystem.Unregister(colliderId).MapToResult();
    }

    public Result Update(TimeSpan deltaTime)
    {
        foreach (var (vehicleId, trackPosition) in vehiclePositionService.TrackPositions)
        {
            if (!_colliders.TryGetValue(vehicleId, out var colliderId))
            {
                continue;
            }

            if (trackSplineService.GetPosition(trackPosition.TrackId, trackPosition.Time)
                .TryPickProblems(out var problems, out var position))
            {
                return problems.Prepend(
                    "Failed to sample spline for vehicle '{0}' collision update", vehicleId);
            }

            var worldMatrix = VehicleWorldMatrix.Compute(trackPosition.Velocity, position);
            var centerMatrix = Matrix4X4.CreateTranslation(_centerOffset);
            var adjustedMatrix = centerMatrix * worldMatrix;

            if (collisionSystem.UpdateTransform(colliderId, adjustedMatrix)
                .TryPickProblems(out problems))
            {
                return problems.Prepend(
                    "Failed to update collider transform for vehicle '{0}'", vehicleId);
            }
        }

        return Result.Success();
    }
}
