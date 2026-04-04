using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Collision;

namespace Olve.Trains.Scenes.GameLogic.Buildings;

public class BuildingCollisionService(
    MeshLoadingManager meshLoadingManager,
    MeshManager meshManager,
    CollisionSystem collisionSystem,
    BuildingService buildingService,
    BuildingBlueprintService buildingBlueprintService,
    BuildingMeshBlueprintService buildingMeshBlueprintService,
    BuildingPositionService buildingPositionService)
{
    private readonly Dictionary<Id<Building>, Id<Collider>> _colliders = new();
    private readonly Dictionary<Id<Collider>, Id<Building>> _reverseColliders = new();

    public Result Register(Id<Building> buildingId)
    {
        if (_colliders.ContainsKey(buildingId))
        {
            return new ResultProblem("Collider already exists for building '{0}'", buildingId);
        }

        if (!buildingService.TryGetBuilding(buildingId, out var building))
        {
            return new ResultProblem("Building not found: '{0}'", buildingId);
        }

        if (!buildingBlueprintService.TryGetBlueprint(building.BlueprintId, out var blueprint))
        {
            return new ResultProblem("Blueprint not found: '{0}'", building.BlueprintId);
        }

        BoxColliderShape shape;
        Matrix4X4<float> adjustedMatrix;

        if (buildingMeshBlueprintService.TryGetProperties(building.BlueprintId, out var meshProps))
        {
            if (meshLoadingManager.LoadMesh(meshProps.MeshPath)
                .TryPickProblems(out var problems, out var meshId))
            {
                return problems.Prepend("Failed to load building mesh for collision");
            }

            if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
            {
                return new ResultProblem("Mesh AABB not found for building '{0}'", buildingId);
            }

            var halfExtents = (localAABB.Max - localAABB.Min) * 0.5f;
            var center = (localAABB.Min + localAABB.Max) * 0.5f;
            shape = new BoxColliderShape(halfExtents);

            var worldMatrix = buildingPositionService.ComputeBuildingWorldMatrix(
                blueprint.Footprint, building.Position, building.BlueprintId);

            // Adjust world matrix to account for mesh center offset
            var centerOffset = Matrix4X4.CreateTranslation(center);
            adjustedMatrix = centerOffset * worldMatrix;
        }
        else
        {
            // No mesh — use footprint-based box collider matching the rendered cube
            var uniformScale = float.Min(blueprint.Footprint.Width,
                float.Min(blueprint.Footprint.Height, blueprint.Footprint.Depth));
            var half = uniformScale / 2f;
            shape = new BoxColliderShape(new Vector3D<float>(half, half, half));

            // Cube center is at (0.5, 0.5, 0.5) in local space before scaling
            var centerOffset = Matrix4X4.CreateTranslation(
                new Vector3D<float>(0.5f, 0.5f, 0.5f));

            adjustedMatrix = centerOffset * buildingPositionService.ComputeBuildingWorldMatrix(
                blueprint.Footprint, building.Position, building.BlueprintId);
        }

        var colliderId = collisionSystem.Register(shape, ColliderGroups.Building, adjustedMatrix);
        _colliders[buildingId] = colliderId;
        _reverseColliders[colliderId] = buildingId;

        return Result.Success();
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (!_colliders.Remove(buildingId, out var colliderId))
        {
            return Result.Success();
        }

        _reverseColliders.Remove(colliderId);
        return collisionSystem.Unregister(colliderId).MapToResult();
    }

    public bool TryGetBuildingId(Id<Collider> colliderId, out Id<Building> buildingId) =>
        _reverseColliders.TryGetValue(colliderId, out buildingId);
}
