using Olve.Utilities.Ids;
using Silk.NET.Maths;

namespace Olve.Engine3D.Physics3D.Collisions;

public readonly record struct RaycastHit(
    Id<Collider> ColliderId,
    Id<ColliderGroup> Group,
    float Distance,
    Vector3D<float> HitPosition);
