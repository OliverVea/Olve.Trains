using Olve.Engine3D.Assets.Entities;
using Olve.Engine3D.Math;

namespace Olve.Engine3D.Physics3D.Collisions;

public class HeightmapColliderShape : IColliderShape
{
    private readonly HeightmapRaycaster _raycaster;
    private readonly AABB _localBounds;

    public HeightmapColliderShape(HeightmapData data)
    {
        _raycaster = new HeightmapRaycaster(data);

        var minHeight = int.MaxValue;
        var maxHeight = int.MinValue;
        foreach (var h in data.Heights)
        {
            if (h < minHeight) minHeight = h;
            if (h > maxHeight) maxHeight = h;
        }

        _localBounds = new AABB(
            new Vector3D<float>(0, minHeight * data.Step, 0),
            new Vector3D<float>(data.Width - 1, maxHeight * data.Step, data.Length - 1));
    }

    public AABB GetWorldAABB(in Matrix4X4<float> worldMatrix)
    {
        return AABBHelper.TransformAABB(_localBounds, worldMatrix);
    }

    public bool TryRaycast(
        in Ray3D<float> ray,
        in Matrix4X4<float> worldMatrix,
        out float distance,
        out Vector3D<float> hitPosition)
    {
        // HeightmapRaycaster does its own bounds checking; identity world matrix assumed
        if (_raycaster.TryRaycast(ray, out var hitPoint))
        {
            distance = (hitPoint.Value - ray.Origin).Length;
            hitPosition = hitPoint.Value;
            return true;
        }

        distance = default;
        hitPosition = default;
        return false;
    }
}
