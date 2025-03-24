using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.Entities;

namespace Olve.Engine3D.Utilities;

public static class GeometryHelper
{
    public static MeshData CreateQuadGeometry()
    {
        Vector3D<float>[] positions = [
            new(-0.5f, -0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(0.5f, 0.5f, 0f),
            new(-0.5f, 0.5f, 0f)
        ];

        Vector3D<float>[] normals =
        [
            new(0, 0, 1),
            new(0, 0, 1),
            new(0, 0, 1),
            new(0, 0, 1)
        ];

        TriangleIndex[] indices =
        [
            new(0, 1, 2),
            new(0, 2, 3),
        ];

        Vector2D<float>[] textureCoordinates =
        [
            new(0, 0),
            new(1, 0),
            new(1, 1),
            new(0, 1)
        ];

        return new MeshData
        {
            Positions = positions,
            Normals = normals,
            Indices = indices,
            TextureCoordinates = textureCoordinates
        };
    }
}