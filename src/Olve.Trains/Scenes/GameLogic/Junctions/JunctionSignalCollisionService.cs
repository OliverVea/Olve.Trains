using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Physics3D.Collisions;
using Olve.Generated.Meshes;
using Olve.Trains.Scenes.GameLogic.Collision;
using Olve.Trains.Scenes.GameLogic.Terrain;
using Olve.Utilities.Collections;

namespace Olve.Trains.Scenes.GameLogic.Junctions;

public class JunctionSignalCollisionService(
    MeshLoadingManager meshLoadingManager,
    MeshManager meshManager,
    CollisionSystem collisionSystem,
    JunctionService junctionService,
    GridService gridService)
{
    private readonly OneToManyLookup<Id<Junction>, Id<Collider>> _colliders = new();

    private BoxColliderShape? _shape;
    private Vector3D<float> _centerOffset;

    public bool TryGetJunctionId(Id<Collider> colliderId, out Id<Junction> junctionId)
    {
        return _colliders.TryGet(colliderId, out junctionId);
    }

    public Result Register(Id<Junction> junctionId)
    {
        if (_colliders.TryGet(junctionId, out _))
        {
            return new ResultProblem("Collider already exists for junction signal '{0}'", junctionId);
        }

        if (_shape is null)
        {
            if (meshLoadingManager.LoadMesh(Meshes.SM_Prop_CrossingLight_01)
                .TryPickProblems(out var loadProblems, out var meshId))
            {
                return loadProblems.Prepend("Failed to load junction signal mesh for collision");
            }

            if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
            {
                return new ResultProblem("Mesh AABB not found for junction signal mesh");
            }

            var halfExtents = (localAABB.Max - localAABB.Min) * 0.5f;
            _centerOffset = (localAABB.Min + localAABB.Max) * 0.5f;
            _shape = new BoxColliderShape(halfExtents);
        }

        if (!junctionService.TryGetJunction(junctionId, out var junction))
        {
            return new ResultProblem("Junction not found: '{0}'", junctionId);
        }

        var junctionWorld = Matrix4X4.CreateScale(0.5f)
                            * Matrix4X4.CreateRotationY(float.Pi)
                            * Matrix4X4.CreateTranslation(0.3f, 0f, 0.3f)
                            * Matrix4X4.CreateTranslation(gridService.ToTileCenter(junction.Position));

        var centerMatrix = Matrix4X4.CreateTranslation(_centerOffset);
        var adjustedMatrix = centerMatrix * junctionWorld;

        var colliderId = collisionSystem.Register(_shape, ColliderGroups.Signal, adjustedMatrix);
        _colliders.Set(junctionId, colliderId, true);

        return Result.Success();
    }

    public Result Unregister(Id<Junction> junctionId)
    {
        if (!_colliders.TryGet(junctionId, out var colliderIds) || colliderIds.Count == 0)
        {
            return new ResultProblem("Could not find collider for signal with junction id '{0}'", junctionId);
        }

        var result = Result.Success();
        foreach (var colliderId in colliderIds)
        {
            result = Result.Concat(result, collisionSystem.Unregister(colliderId).MapToResult());
        }

        _colliders.Remove(junctionId);

        return result;
    }
}
