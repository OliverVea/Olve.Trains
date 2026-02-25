namespace Olve.Engine3D.Rendering.Primitives;

/// <summary>
/// Unit quad with corners at (0,0) and (1,1).
/// 6 vertices (2 triangles), no indices.
/// </summary>
public static class UnitQuad
{
    public const int VertexCount = 6;

    public static void GetPositions(Span<Vector2D<float>> positions)
    {
        positions[0] = new(0f, 0f);
        positions[1] = new(1f, 0f);
        positions[2] = new(1f, 1f);
        positions[3] = new(0f, 0f);
        positions[4] = new(1f, 1f);
        positions[5] = new(0f, 1f);
    }

    public static void Populate<T>(Span<T> vertices)
        where T : struct, IWithPosition2D<T>
    {
        Span<Vector2D<float>> positions = stackalloc Vector2D<float>[VertexCount];
        GetPositions(positions);

        for (var i = 0; i < VertexCount; i++)
        {
            vertices[i] = T.WithPosition(vertices[i], positions[i]);
        }
    }
}
