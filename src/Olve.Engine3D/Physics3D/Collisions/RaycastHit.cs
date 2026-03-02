using Olve.Utilities.Ids;

namespace Olve.Engine3D.Physics3D.Collisions;

public readonly record struct RaycastHit(
    Id<Collider> ColliderId,
    Id<ColliderGroup> Group,
    float Distance);
