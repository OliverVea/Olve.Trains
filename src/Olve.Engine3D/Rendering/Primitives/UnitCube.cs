namespace Olve.Engine3D.Rendering.Primitives;

/// <summary>
/// Unit cube with corners at (0,0,0) and (1,1,1).
/// 24 vertices (4 per face, each with outward-facing normal), 36 indices (two triangles per face).
/// </summary>
public static class UnitCube
{
    public const int VertexCount = 24;
    public const int IndexCount = 36;

    public static void GetVertices(
        Span<Vector3D<float>> positions,
        Span<Vector3D<float>> normals)
    {
        var i = 0;

        // Front face (z = 1)
        Vector3D<float> n = new(0, 0, 1);
        positions[i] = new(0, 0, 1); normals[i++] = n;
        positions[i] = new(1, 0, 1); normals[i++] = n;
        positions[i] = new(1, 1, 1); normals[i++] = n;
        positions[i] = new(0, 1, 1); normals[i++] = n;

        // Back face (z = 0)
        n = new(0, 0, -1);
        positions[i] = new(1, 0, 0); normals[i++] = n;
        positions[i] = new(0, 0, 0); normals[i++] = n;
        positions[i] = new(0, 1, 0); normals[i++] = n;
        positions[i] = new(1, 1, 0); normals[i++] = n;

        // Right face (x = 1)
        n = new(1, 0, 0);
        positions[i] = new(1, 0, 1); normals[i++] = n;
        positions[i] = new(1, 0, 0); normals[i++] = n;
        positions[i] = new(1, 1, 0); normals[i++] = n;
        positions[i] = new(1, 1, 1); normals[i++] = n;

        // Left face (x = 0)
        n = new(-1, 0, 0);
        positions[i] = new(0, 0, 0); normals[i++] = n;
        positions[i] = new(0, 0, 1); normals[i++] = n;
        positions[i] = new(0, 1, 1); normals[i++] = n;
        positions[i] = new(0, 1, 0); normals[i++] = n;

        // Top face (y = 1)
        n = new(0, 1, 0);
        positions[i] = new(0, 1, 1); normals[i++] = n;
        positions[i] = new(1, 1, 1); normals[i++] = n;
        positions[i] = new(1, 1, 0); normals[i++] = n;
        positions[i] = new(0, 1, 0); normals[i++] = n;

        // Bottom face (y = 0)
        n = new(0, -1, 0);
        positions[i] = new(0, 0, 0); normals[i++] = n;
        positions[i] = new(1, 0, 0); normals[i++] = n;
        positions[i] = new(1, 0, 1); normals[i++] = n;
        positions[i] = new(0, 0, 1); normals[i] = n;
    }

    public static void Populate<T>(Span<T> vertices)
        where T : struct, IWithPosition3D<T>, IWithNormal3D<T>
    {
        Span<Vector3D<float>> positions = stackalloc Vector3D<float>[VertexCount];
        Span<Vector3D<float>> normals = stackalloc Vector3D<float>[VertexCount];
        GetVertices(positions, normals);

        for (var i = 0; i < VertexCount; i++)
        {
            vertices[i] = T.WithPosition(vertices[i], positions[i]);
            vertices[i] = T.WithNormal(vertices[i], normals[i]);
        }
    }

    public static void GetIndices(Span<uint> indices)
    {
        for (uint face = 0; face < 6; face++)
        {
            var offset = face * 4;
            var i = (int)(face * 6);
            indices[i] = offset;
            indices[i + 1] = offset + 1;
            indices[i + 2] = offset + 2;
            indices[i + 3] = offset;
            indices[i + 4] = offset + 2;
            indices[i + 5] = offset + 3;
        }
    }
}
