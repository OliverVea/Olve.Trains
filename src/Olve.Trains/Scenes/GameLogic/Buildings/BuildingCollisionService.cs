using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;

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

        if (!buildingMeshBlueprintService.TryGetProperties(building.BlueprintId, out var meshProps)
            || meshProps.MeshPath is not { } meshPath)
        {
            return Result.Success();
        }

        if (meshLoadingManager.LoadMesh(meshPath)
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
        var shape = new BoxColliderShape(halfExtents);

        var worldMatrix = buildingPositionService.ComputeBuildingWorldMatrix(
            blueprint.Footprint, building.Position, building.BlueprintId);

        // Adjust world matrix to account for mesh center offset
        var centerOffset = Matrix4X4.CreateTranslation(center);
        var adjustedMatrix = centerOffset * worldMatrix;

        var colliderId = collisionSystem.Register(shape, ColliderGroups.Building, adjustedMatrix);
        _colliders[buildingId] = colliderId;

        return Result.Success();
    }

    public Result Unregister(Id<Building> buildingId)
    {
        if (!_colliders.Remove(buildingId, out var colliderId))
        {
            return Result.Success();
        }

        return collisionSystem.Unregister(colliderId).MapToResult();
    }
}
