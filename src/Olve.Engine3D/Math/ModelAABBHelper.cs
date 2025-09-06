using Olve.Engine3D.Rendering.Entities;

namespace Olve.Engine3D.Math;

public readonly record struct AABB(Vector3D<float> Min, Vector3D<float> Max);

public static class AABBExtensions
{
    public static bool Intersects(this in AABB a, in AABB b)
    {
        return !(a.Max.X < b.Min.X || a.Min.X > b.Max.X ||
                 a.Max.Y < b.Min.Y || a.Min.Y > b.Max.Y ||
                 a.Max.Z < b.Min.Z || a.Min.Z > b.Max.Z);
    }

    public static bool Contains(this in AABB box, in Vector3D<float> p)
    {
        return p.X >= box.Min.X && p.X <= box.Max.X &&
               p.Y >= box.Min.Y && p.Y <= box.Max.Y &&
               p.Z >= box.Min.Z && p.Z <= box.Max.Z;
    }
}

public static class AABBHelper
{
    public static Result<AABB> ComputeAABBOfMesh(MeshData meshData, float scale = 1f) 
        => ComputeAABBOfMesh(meshData, new Vector3D<float>(scale, scale, scale));

    public static Result<AABB> ComputeAABBOfMesh(MeshData meshData, Vector3D<float> scale)
    {
        if (meshData.Positions.Length == 0)
        {
            return new ResultProblem("Mesh has no vertices");
        }
        
        if (scale.X == 0f || scale.Y == 0f || scale.Z == 0f)
        {
            return new ResultProblem("Scale cannot be zero");
        }
        
        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var vertex in meshData.Positions)
        {
            var scaledVertex = vertex * scale;
            
            minX = MathF.Min(minX, scaledVertex.X);
            minY = MathF.Min(minY, scaledVertex.Y);
            minZ = MathF.Min(minZ, scaledVertex.Z);
            
            maxX = MathF.Max(maxX, scaledVertex.X);
            maxY = MathF.Max(maxY, scaledVertex.Y);
            maxZ = MathF.Max(maxZ, scaledVertex.Z);
        }

        return new AABB(
            new Vector3D<float>(minX, minY, minZ),
            new Vector3D<float>(maxX, maxY, maxZ));
    }

    public static Result<float> GetUniformScaleToFitInside(MeshData meshData, AABB target)
    {
        if (GetHeterogeneousScaleToFitInside(meshData, target).TryPickProblems(out var problems, out var heterogeneousScale))
        {
            return problems.Prepend("Failed to compute AABB of mesh");
        }
        
        return float.Min(heterogeneousScale.X, float.Min(heterogeneousScale.Y, heterogeneousScale.Z));
    }

    public static Result<Vector3D<float>> GetHeterogeneousScaleToFitInside(MeshData meshData, AABB target)
    {
        if (ComputeAABBOfMesh(meshData).TryPickProblems(out var problems, out var aabb))
        {
            return problems.Prepend("Failed to compute AABB of mesh");
        }
        
        var deltaX = aabb.Max.X - aabb.Min.X;
        var deltaY = aabb.Max.Y - aabb.Min.Y;
        var deltaZ = aabb.Max.Z - aabb.Min.Z;

        if (deltaX == 0f || deltaY == 0f || deltaZ == 0f)
        {
            return new ResultProblem("Mesh AABB has zero extent along one or more axes.");
        }
        
        var scaleX = (target.Max.X - target.Min.X) / (aabb.Max.X - aabb.Min.X);
        var scaleY = (target.Max.Y - target.Min.Y) / (aabb.Max.Y - aabb.Min.Y);
        var scaleZ = (target.Max.Z - target.Min.Z) / (aabb.Max.Z - aabb.Min.Z);
        
        return new Vector3D<float>(scaleX, scaleY, scaleZ);
    }
}