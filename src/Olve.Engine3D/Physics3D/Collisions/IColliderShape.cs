using Olve.Engine3D.Math;

namespace Olve.Engine3D.Physics3D.Collisions;

public interface IColliderShape
{
    AABB GetWorldAABB(in Matrix4X4<float> worldMatrix);

    bool TryRaycast(
        in Ray3D<float> ray,
        in Matrix4X4<float> worldMatrix,
        out float distance,
        out Vector3D<float> hitPosition);
}
