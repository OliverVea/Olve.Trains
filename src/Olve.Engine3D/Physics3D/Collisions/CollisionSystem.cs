using Olve.Engine3D.Assets.Meshes;
using Olve.Engine3D.Math;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Physics3D.Collisions;

public class CollisionSystem(MeshManager meshManager)
{
    private readonly record struct ColliderEntry(
        Id<Mesh> MeshId,
        Id<ColliderGroup> Group,
        AABB LocalAABB,
        AABB WorldAABB);

    private readonly Dictionary<Id<Collider>, ColliderEntry> _colliders = new();

    public Result<Id<Collider>> RegisterMeshCollider(
        Id<Mesh> meshId,
        Id<ColliderGroup> group,
        Matrix4X4<float> worldMatrix)
    {
        if (!meshManager.TryGetLocalAABB(meshId, out var localAABB))
        {
            return new ResultProblem("Mesh '{0}' not found in MeshManager", meshId);
        }

        var worldAABB = AABBHelper.TransformAABB(localAABB, worldMatrix);
        var colliderId = Id.New<Collider>();

        _colliders[colliderId] = new ColliderEntry(meshId, group, localAABB, worldAABB);

        return colliderId;
    }

    public Result UpdateTransform(Id<Collider> colliderId, Matrix4X4<float> worldMatrix)
    {
        if (!_colliders.TryGetValue(colliderId, out var entry))
        {
            return new ResultProblem("Collider '{0}' not found", colliderId);
        }

        var worldAABB = AABBHelper.TransformAABB(entry.LocalAABB, worldMatrix);
        _colliders[colliderId] = entry with { WorldAABB = worldAABB };

        return Result.Success();
    }

    public DeletionResult Unregister(Id<Collider> colliderId)
    {
        if (!_colliders.ContainsKey(colliderId))
        {
            return DeletionResult.NotFound();
        }

        _colliders.Remove(colliderId);
        return DeletionResult.Success();
    }

    public IReadOnlyList<RaycastHit> Raycast(Ray3D<float> ray)
    {
        var hits = new List<RaycastHit>();

        foreach (var (colliderId, entry) in _colliders)
        {
            if (entry.WorldAABB.TryIntersectRay(ray, out var distance))
            {
                hits.Add(new RaycastHit(colliderId, entry.Group, distance));
            }
        }

        hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return hits;
    }

    public IReadOnlyList<RaycastHit> Raycast(Ray3D<float> ray, Id<ColliderGroup> group)
    {
        var hits = new List<RaycastHit>();

        foreach (var (colliderId, entry) in _colliders)
        {
            if (entry.Group != group) continue;

            if (entry.WorldAABB.TryIntersectRay(ray, out var distance))
            {
                hits.Add(new RaycastHit(colliderId, entry.Group, distance));
            }
        }

        hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return hits;
    }
}
