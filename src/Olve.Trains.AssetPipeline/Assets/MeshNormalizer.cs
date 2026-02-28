using Olve.Engine3D.Assets.Entities;
using Silk.NET.Maths;

namespace Olve.Trains.AssetPipeline.Assets;

public static class MeshNormalizer
{
    /// <summary>
    /// Normalizes mesh positions in-place:
    /// - Uniform scale to fit within a 1x1x1 bounding box
    /// - Centered on the origin in X and Z
    /// - Bottom of the mesh sits on Y=0
    /// </summary>
    public static void Normalize(MeshData meshData)
    {
        if (meshData.Positions.Length == 0) return;

        float minX = float.MaxValue, minY = float.MaxValue, minZ = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue, maxZ = float.MinValue;

        foreach (var pos in meshData.Positions)
        {
            minX = MathF.Min(minX, pos.X);
            minY = MathF.Min(minY, pos.Y);
            minZ = MathF.Min(minZ, pos.Z);
            maxX = MathF.Max(maxX, pos.X);
            maxY = MathF.Max(maxY, pos.Y);
            maxZ = MathF.Max(maxZ, pos.Z);
        }

        var extentX = maxX - minX;
        var extentY = maxY - minY;
        var extentZ = maxZ - minZ;

        var maxExtent = MathF.Max(extentX, MathF.Max(extentY, extentZ));
        if (maxExtent == 0f) return;

        var scale = 1f / maxExtent;

        var offsetX = -(minX + maxX) / 2f;
        var offsetY = -minY;
        var offsetZ = -(minZ + maxZ) / 2f;

        for (var i = 0; i < meshData.Positions.Length; i++)
        {
            var pos = meshData.Positions[i];
            meshData.Positions[i] = new Vector3D<float>(
                (pos.X + offsetX) * scale,
                (pos.Y + offsetY) * scale,
                (pos.Z + offsetZ) * scale);
        }
    }
}
