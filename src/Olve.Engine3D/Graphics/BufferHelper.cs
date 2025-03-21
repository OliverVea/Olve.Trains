namespace Olve.Engine3D.Graphics;

public static class BufferHelper
{
    public static void CopyTo(Vector3D<float>[] vertices, Span<float> buffer, int stride, int offset = 0)
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
            buffer[j + 2] = vertices[i].Z;
        }
    }

    public static void CopyTo(TriangleIndex[] indices, Span<uint> buffer)
    {
        for (var i = 0; i < indices.Length; i++)
        {
            buffer[i * 3] = indices[i].A;
            buffer[i * 3 + 1] = indices[i].B;
            buffer[i * 3 + 2] = indices[i].C;
        }
    }

    public static void CopyTo(Matrix4X4<float> matrix, Span<float> buffer)
    {
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                buffer[i * 4 + j] = matrix[i, j];
            }
        }
    }
}