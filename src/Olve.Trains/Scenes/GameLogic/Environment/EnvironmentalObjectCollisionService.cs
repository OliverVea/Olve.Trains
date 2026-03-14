using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Trains.Scenes.GameLogic.Collision;

namespace Olve.Trains.Scenes.GameLogic.Environment;

public class EnvironmentalObjectCollisionService(
    CollisionSystem collisionSystem,
    MeshLoadingManager meshLoadingManager,
    MeshManager meshManager,
    EnvironmentalObjectService environmentalObjectService,
    EnvironmentalObjectBlueprintService blueprintService)
{
    private readonly Dictionary<Id<EnvironmentalObject>, Id<Collider>> _colliders = new();
    private readonly Dictionary<Id<Collider>, Id<EnvironmentalObject>> _reverseColliders = new();

    public Result Register(Id<EnvironmentalObject> objectId)
    {
        if (_colliders.ContainsKey(objectId))
        {
            return new ResultProblem("Collider already exists for environmental object '{0}'", objectId);
        }

        if (!environmentalObjectService.TryGetObject(objectId, out var obj))
        {
            return new ResultProblem("Environmental object not found: '{0}'", objectId);
        }

        var meshPath = obj.MeshPath;
        if (meshPath is null
            && blueprintService.TryGetBlueprint(obj.BlueprintId, out var blueprint))
        {
            meshPath = blueprint.MeshPath;
        }

        if (meshPath is not { } resolvedMeshPath)
        {
            return Result.Success();
        }

        if (meshLoadingManager.LoadMesh(resolvedMeshPath)
            .TryPickProblems(out var problems, out var meshId))
        {
            return problems.Prepend("Failed to load mesh for environmental object collision");
        }

        if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
        {
            return new ResultProblem("Mesh AABB not found for environmental object '{0}'", objectId);
        }

        var halfExtents = (localAABB.Max - localAABB.Min) * 0.5f;
        var center = (localAABB.Min + localAABB.Max) * 0.5f;
        var shape = new BoxColliderShape(halfExtents);

        var worldMatrix = obj.Position.ToMatrix4X4();
        var centerOffset = Matrix4X4.CreateTranslation(center);
        var adjustedMatrix = centerOffset * worldMatrix;

        var colliderId = collisionSystem.Register(shape, ColliderGroups.Environment, adjustedMatrix);
        _colliders[objectId] = colliderId;
        _reverseColliders[colliderId] = objectId;

        return Result.Success();
    }

    public Result Unregister(Id<EnvironmentalObject> objectId)
    {
        if (!_colliders.Remove(objectId, out var colliderId))
        {
            return Result.Success();
        }

        _reverseColliders.Remove(colliderId);
        return collisionSystem.Unregister(colliderId).MapToResult();
    }

    public bool TryGetObjectId(Id<Collider> colliderId, out Id<EnvironmentalObject> objectId) =>
        _reverseColliders.TryGetValue(colliderId, out objectId);
}
