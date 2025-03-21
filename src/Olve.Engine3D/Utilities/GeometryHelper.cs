using Olve.Engine3D.Graphics;
using Olve.Engine3D.Graphics.OpenGL;

namespace Olve.Engine3D.Utilities;

public static class GeometryHelper
{
    public static GeometryData<TriangleIndex> CreateQuadGeometry(Vector3D<float>[]? vertices = null, TriangleIndex[]? indices = null)
    {
        vertices ??= [
            new(-0.5f, -0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(0.5f, 0.5f, 0f),
            new(-0.5f, 0.5f, 0f)
        ];
        indices ??= [
            new(0, 1, 2),
            new(0, 2, 3),
        ];

        return new GeometryData<TriangleIndex>
        {
            Vertices = vertices,
            Indices = indices
        };
    }

    public static GeometryData<LineIndex> CreateQuadWireframe(Vector3D<float>[]? vertices = null, LineIndex[]? indices = null)
    {
        vertices ??= [
            new(-0.5f, -0.5f, 0f),
            new(0.5f, -0.5f, 0f),
            new(0.5f, 0.5f, 0f),
            new(-0.5f, 0.5f, 0f)
        ];
        indices ??=
        [
            new(0, 1),
            new(1, 2),
            new(2, 3),
            new(3, 0),
            new LineIndex(0, 2),
        ];

        return new GeometryData<LineIndex>
        {
            Vertices = vertices,
            Indices = indices
        };
    }

    public static GeometryData<LineIndex> CreateWorldAxes(Vector3D<float>[]? vertices = null, LineIndex[]? indices = null)
    {
        vertices ??=
        [
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 1, 0),
            new(0, 0, 1),
        ];
        indices ??=
        [
            new(0, 1),
            new(0, 2),
            new(0, 3),
        ];

        return new GeometryData<LineIndex>
        {
            Vertices = vertices,
            Indices = indices
        };
    }
}