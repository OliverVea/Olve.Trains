using System.Buffers;

namespace Olve.Engine3D.Graphics;

public static class BufferHelper
{
    private const int MaxStackAllocationSize = 2048;

    public static void UsingSpan<T>(int spanSize, Action<Span<T>> action, int maxAllocationSize = MaxStackAllocationSize) where T : unmanaged
    {
        T[]? array = null;

        try
        {
            array = spanSize > maxAllocationSize ? ArrayPool<T>.Shared.Rent(spanSize) : null;
            var span = array is not null ? array.AsSpan(0, spanSize) : stackalloc T[spanSize];

            action(span);
        }
        finally
        {
            if (array is not null)
            {
                ArrayPool<T>.Shared.Return(array);
            }
        }
    }

    public static void CopyTo<T>(Vector3D<T>[] vertices, Span<T> buffer, int stride = 3, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
            buffer[j + 2] = vertices[i].Z;
        }
    }

    public static void CopyTo<T>(Vector4D<T>[] vertices, Span<T> buffer, int stride = 4, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
            buffer[j + 2] = vertices[i].Z;
            buffer[j + 3] = vertices[i].Z;
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

    public static void CopyTo<T>(Matrix4X4<T> matrix, Span<T> buffer) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                buffer[i * 4 + j] = matrix[i, j];
            }
        }
    }

    public static void CopyTo<T>(Vector2D<T>[] vertices, Span<T> buffer, int stride, int offset = 0) where T : unmanaged, IFormattable, IEquatable<T>, IComparable<T>
    {
        for (var i = 0; i < vertices.Length; i += 1)
        {
            var j = i * stride + offset;
            buffer[j] = vertices[i].X;
            buffer[j + 1] = vertices[i].Y;
        }
    }
}