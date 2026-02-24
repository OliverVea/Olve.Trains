using Olve.Engine3D.Assets.Entities;

namespace Olve.Engine3D.Rendering;

public static class VertexPopulateExtensions
{
    public static void Populate<T>(this MeshData meshData, Span<T> vertices)
        where T : struct, IWithPosition3D<T>, IWithNormal3D<T>, IWithTexCoords2D<T>
    {
        for (var i = 0; i < meshData.VertexCount; i++)
        {
            vertices[i] = T.WithPosition(vertices[i], meshData.Positions[i]);
            vertices[i] = T.WithNormal(vertices[i], meshData.Normals[i]);
            vertices[i] = T.WithTexCoords(vertices[i], meshData.TextureCoordinates[i]);
        }
    }

    public static void Populate<T>(this LineStripData lineStripData, Span<T> vertices)
        where T : struct, IWithPosition3D<T>, IWithColor3D<T>
    {
        for (var i = 0; i < lineStripData.VertexCount; i++)
        {
            vertices[i] = T.WithPosition(vertices[i], lineStripData.Positions[i]);
            vertices[i] = T.WithColor(vertices[i], lineStripData.Colors[i]);
        }
    }
}
