using System.Diagnostics.CodeAnalysis;

namespace Olve.Engine3D.Physics3D.Collisions;

public static class RayExtensions
{
    public static bool TryEvaluateWithY(this Ray3D<float> ray, float y, [NotNullWhen(true)] out Vector3D<float>? point)
    {
        if (float.Abs(ray.Direction.Y) <= 1e-10)
        {
            point = null;
            return false;
        }

        var t = (y - ray.Origin.Y) / ray.Direction.Y;

        point = new Vector3D<float>(
            ray.Origin.X + t * ray.Direction.X,
            y,
            ray.Origin.Z + t * ray.Direction.Z);

        return true;
    }
}