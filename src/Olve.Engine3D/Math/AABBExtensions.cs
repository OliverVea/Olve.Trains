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
    }

}