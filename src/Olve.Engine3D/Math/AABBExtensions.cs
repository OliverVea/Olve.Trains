namespace Olve.Engine3D.Math;

public static class AABBExtensions
{
    extension(in AABB a)
    {
        public bool Intersects(in AABB b)
        {
            return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X ||
                     a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y ||
                     a.Max.Z < b.Min.Z || a.Min.Z > b.Max.Z);
        }

        public bool Contains(in Vector3D<float> p)
        {
            return p.X >= a.Min.X && p.X <= a.Max.X &&
                   p.Y >= a.Min.Y && p.Y <= a.Max.Y &&
                   p.Z >= a.Min.Z && p.Z <= a.Max.Z;
        }

        public bool TryIntersectRay(in Ray3D<float> ray, out float tMin)
        {
            var invDirX = 1f / ray.Direction.X;
            var invDirY = 1f / ray.Direction.Y;
            var invDirZ = 1f / ray.Direction.Z;

            var t1X = (a.Min.X - ray.Origin.X) * invDirX;
            var t2X = (a.Max.X - ray.Origin.X) * invDirX;
            var t1Y = (a.Min.Y - ray.Origin.Y) * invDirY;
            var t2Y = (a.Max.Y - ray.Origin.Y) * invDirY;
            var t1Z = (a.Min.Z - ray.Origin.Z) * invDirZ;
            var t2Z = (a.Max.Z - ray.Origin.Z) * invDirZ;

            var tEnter = float.Max(float.Max(float.Min(t1X, t2X), float.Min(t1Y, t2Y)), float.Min(t1Z, t2Z));
            var tExit = float.Min(float.Min(float.Max(t1X, t2X), float.Max(t1Y, t2Y)), float.Max(t1Z, t2Z));

            if (tExit < 0f || tEnter > tExit)
            {
                tMin = default;
                return false;
            }

            tMin = tEnter >= 0f ? tEnter : tExit;
            return true;
        }
    }

}