using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Engine3D.Scenes;
using Olve.Generated.Meshes;
using Olve.Trains.Scenes.GameLogic.Tracks;

namespace Olve.Trains.Scenes.GameLogic.Vehicles;

public class VehicleCollisionService(
    MeshLoadingManager meshLoadingManager,
    CollisionSystem collisionSystem,
    VehiclePositionService vehiclePositionService,
    TrackSplineService trackSplineService)
    : ISceneService
{
    public int Priority => SceneServicePriority.FromDependencies([trackSplineService, vehiclePositionService]);

    private readonly Dictionary<Id<Vehicle>, Id<Collider>> _colliders = new();
    private Id<Mesh> _meshId;

    public Result Load()
    {
        if (meshLoadingManager.LoadMesh(Meshes.SM_Veh_Bullet_01)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load vehicle mesh for collision");
        }

        _meshId = meshId;
        return Result.Success();
    }

    public Result Register(Id<Vehicle> vehicleId)
    {
        if (_colliders.ContainsKey(vehicleId))
        {
            return new ResultProblem("Collider already exists for vehicle '{0}'", vehicleId);
        }

        if (collisionSystem.RegisterMeshCollider(_meshId, ColliderGroups.Vehicle, Matrix4X4<float>.Identity)
            .TryPickProblems(out var problems, out var colliderId))
        {
            return problems.Prepend("Failed to register collider for vehicle '{0}'", vehicleId);
        }

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

            if (collisionSystem.UpdateTransform(colliderId, worldMatrix)
                .TryPickProblems(out problems))
            {
                return problems.Prepend(
                    "Failed to update collider transform for vehicle '{0}'", vehicleId);
            }
        }

        return Result.Success();
    }
}
