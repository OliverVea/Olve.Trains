using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;
using Olve.Engine3D.Systems;
using Olve.Utilities.Ids;

namespace Olve.Engine3D.Physics3D.Collisions;

public class CollisionSystem
{
    private readonly record struct ColliderEntry(
        IColliderShape Shape,
        Id<ColliderGroup> Group,
        AABB WorldAABB,
        Matrix4X4<float> WorldMatrix);

    private readonly Dictionary<Id<Collider>, ColliderEntry> _colliders = new();

    public Event<Id<Collider>> OnColliderRegistered { get; } = new();
    public Event<Id<Collider>> OnColliderUnregistered { get; } = new();
    public Event<Id<Collider>> OnColliderTransformUpdated { get; } = new();

    public IEnumerable<Id<Collider>> ColliderIds => _colliders.Keys;

    public Id<Collider> Register(
        IColliderShape shape,
        Id<ColliderGroup> group,
        Matrix4X4<float> worldMatrix)
    {
        var worldAABB = shape.GetWorldAABB(worldMatrix);
        var colliderId = Id.New<Collider>();

        _colliders[colliderId] = new ColliderEntry(shape, group, worldAABB, worldMatrix);

        OnColliderRegistered.Invoke(colliderId);

        return colliderId;
    }

    public Id<Collider> RegisterHeightmapCollider(HeightmapData data, Id<ColliderGroup> group)
    {
        var shape = new HeightmapColliderShape(data);
        return Register(shape, group, Matrix4X4<float>.Identity);
    }

    public Result UpdateTransform(Id<Collider> colliderId, Matrix4X4<float> worldMatrix)
    {
        if (!_colliders.TryGetValue(colliderId, out var entry))
        {
            return new ResultProblem("Collider '{0}' not found", colliderId);
        }

        var worldAABB = entry.Shape.GetWorldAABB(worldMatrix);
        _colliders[colliderId] = entry with { WorldAABB = worldAABB, WorldMatrix = worldMatrix };

        OnColliderTransformUpdated.Invoke(colliderId);

        return Result.Success();
    }

    public DeletionResult Unregister(Id<Collider> colliderId)
    {
        if (!_colliders.Remove(colliderId))
        {
            return DeletionResult.NotFound();
        }

        OnColliderUnregistered.Invoke(colliderId);

        return DeletionResult.Success();
    }

    public bool TryGetColliderInfo(
        Id<Collider> colliderId,
        out IColliderShape shape,
        out Matrix4X4<float> worldMatrix,
        out AABB worldAABB)
    {
        if (_colliders.TryGetValue(colliderId, out var entry))
        {
            shape = entry.Shape;
            worldMatrix = entry.WorldMatrix;
            worldAABB = entry.WorldAABB;
            return true;
        }

        shape = default!;
        worldMatrix = default;
        worldAABB = default;
        return false;
    }

    public IReadOnlyList<RaycastHit> Raycast(Ray3D<float> ray)
    {
        var hits = new List<RaycastHit>();

        foreach (var (colliderId, entry) in _colliders)
        {
            if (TryHit(ray, colliderId, entry, out var hit))
            {
                hits.Add(hit);
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

            if (TryHit(ray, colliderId, entry, out var hit))
            {
                hits.Add(hit);
            }
        }

        hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return hits;
    }

    private static bool TryHit(
        Ray3D<float> ray,
        Id<Collider> colliderId,
        in ColliderEntry entry,
        out RaycastHit hit)
    {
        // Broad phase: test against world-space AABB
        if (!entry.WorldAABB.TryIntersectRay(ray, out _))
        {
            hit = default;
            return false;
        }

        // Narrow phase: delegate to shape
        if (entry.Shape.TryRaycast(ray, entry.WorldMatrix, out var distance, out var hitPosition))
        {
            hit = new RaycastHit(colliderId, entry.Group, distance, hitPosition);
            return true;
        }

        hit = default;
        return false;
    }
}
